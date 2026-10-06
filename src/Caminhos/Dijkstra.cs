namespace Conde.Caminhos;

/// <summary>
/// O resultado de um algoritmo de caminho minimo, com o custo contado.
///
/// O custo e em RELAXAMENTOS e em retiradas da fila, nunca em segundos. Contar
/// operacao da o mesmo numero no Linux, no Windows e no macOS, e segundo nao da.
/// </summary>
public sealed record Resultado(long[] Distancia, int[] Anterior, long Relaxamentos, long Retiradas)
{
    public bool Alcanca(int vertice) => Distancia[vertice] < ForcaBruta.Infinito;

    /// <summary>O caminho ate um vertice, refeito pelos antecessores.</summary>
    public List<int> Caminho(int destino)
    {
        var caminho = new List<int>();
        if (!Alcanca(destino)) return caminho;

        for (var v = destino; v != -1; v = Anterior[v]) caminho.Add(v);
        caminho.Reverse();

        return caminho;
    }

    /// <summary>
    /// O peso do caminho refeito, somado pelas arestas.
    ///
    /// Ele precisa bater com a distancia da tabela. Separar as duas coisas pega o
    /// erro em que a tabela esta certa e o vetor de antecessores aponta para
    /// outro lugar, que e um bug que passa por qualquer teste que so olhe
    /// distancia.
    /// </summary>
    public long PesoDoCaminho(Grafo grafo, int destino)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var caminho = Caminho(destino);
        if (caminho.Count == 0) return ForcaBruta.Infinito;

        long soma = 0;

        for (var i = 1; i < caminho.Count; i++)
        {
            var aresta = grafo.Saindo(caminho[i - 1]).Where(a => a.Para == caminho[i]).ToList();
            if (aresta.Count == 0) return ForcaBruta.Infinito;

            soma += aresta.Min(a => a.Peso);
        }

        return soma;
    }
}

/// <summary>
/// DIJKSTRA, de 1959: sempre expandir o vertice mais proximo ainda nao expandido.
///
/// Ele e o algoritmo de caminho minimo que todo mundo escreve, e ele e correto
/// SOMENTE com pesos nao negativos. A razao e uma frase: quando um vertice sai da
/// fila, o algoritmo declara que a distancia dele esta pronta, e isso so e verdade
/// se nenhum caminho que ainda nao foi explorado pode encurtar o que ja foi.
///
/// Com uma aresta negativa, essa frase e falsa, e o algoritmo NAO QUEBRA: ele
/// devolve um numero errado, com cara de resposta. E isso que as medidas daqui
/// mostram, com o juiz ao lado.
/// </summary>
public static class Dijkstra
{
    public static Resultado De(Grafo grafo, int origem)
    {
        ArgumentNullException.ThrowIfNull(grafo);

        var distancia = new long[grafo.Vertices];
        var anterior = new int[grafo.Vertices];
        var pronto = new bool[grafo.Vertices];

        Array.Fill(distancia, ForcaBruta.Infinito);
        Array.Fill(anterior, -1);
        distancia[origem] = 0;

        var fila = new Monte(grafo.Vertices);
        fila.Por(origem, 0);

        long relaxamentos = 0;
        long retiradas = 0;

        while (fila.Quantos > 0)
        {
            var (vertice, peso) = fila.Tirar();
            retiradas++;

            // a entrada velha de um vertice que ja saiu: o monte nao mexe em
            // chave, ele empilha
            if (pronto[vertice] || peso > distancia[vertice]) continue;
            pronto[vertice] = true;

            foreach (var aresta in grafo.Saindo(vertice))
            {
                relaxamentos++;

                var soma = distancia[vertice] + aresta.Peso;
                if (soma >= distancia[aresta.Para]) continue;

                distancia[aresta.Para] = soma;
                anterior[aresta.Para] = vertice;
                fila.Por(aresta.Para, soma);
            }
        }

        return new Resultado(distancia, anterior, relaxamentos, retiradas);
    }

    /// <summary>
    /// Dijkstra com POTENCIAIS, que e a peca do algoritmo de Johnson.
    ///
    /// O peso usado e w(u,v) + h(u) - h(v). Se os potenciais vem de um
    /// Bellman-Ford, todo peso novo e nao negativo e Dijkstra volta a valer, com a
    /// distancia original recuperada no fim.
    /// </summary>
    public static Resultado ComPotencial(Grafo grafo, int origem, long[] potencial)
    {
        ArgumentNullException.ThrowIfNull(grafo);
        ArgumentNullException.ThrowIfNull(potencial);

        var ajustado = new Grafo(grafo.Vertices);

        foreach (var a in grafo.Todas)
        {
            if (potencial[a.De] >= ForcaBruta.Infinito || potencial[a.Para] >= ForcaBruta.Infinito) continue;
            ajustado.Ligar(a.De, a.Para, a.Peso + potencial[a.De] - potencial[a.Para]);
        }

        var resultado = De(ajustado, origem);
        var distancia = new long[grafo.Vertices];

        for (var v = 0; v < grafo.Vertices; v++)
            distancia[v] = resultado.Distancia[v] >= ForcaBruta.Infinito
                ? ForcaBruta.Infinito
                : resultado.Distancia[v] - potencial[origem] + potencial[v];

        return resultado with { Distancia = distancia };
    }
}

/// <summary>
/// O MONTE BINARIO, escrito aqui.
///
/// A fila de prioridade da biblioteca tem tudo que falta aqui e ela nao conta
/// nada. Com o monte proprio, a medida de "quantas retiradas" e exata e igual nos
/// tres sistemas, e o leitor ve a estrutura que decide a complexidade do
/// algoritmo em vez de uma caixa fechada.
///
/// Ele NAO mexe em chave: quando a distancia de um vertice melhora, uma entrada
/// nova entra e a velha fica. A entrada velha e descartada na saida. Isso gasta
/// mais memoria e e muito mais simples, e e o que quase toda implementacao de
/// verdade faz.
/// </summary>
public sealed class Monte
{
    private (int Vertice, long Peso)[] itens;

    public Monte(int capacidade) => itens = new (int, long)[Math.Max(4, capacidade)];

    public int Quantos { get; private set; }

    public void Por(int vertice, long peso)
    {
        if (Quantos == itens.Length) Array.Resize(ref itens, itens.Length * 2);

        itens[Quantos] = (vertice, peso);
        Subir(Quantos);
        Quantos++;
    }

    public (int Vertice, long Peso) Tirar()
    {
        if (Quantos == 0) throw new InvalidOperationException("monte vazio");

        var topo = itens[0];
        Quantos--;
        itens[0] = itens[Quantos];
        Descer(0);

        return topo;
    }

    private void Subir(int i)
    {
        while (i > 0)
        {
            var pai = (i - 1) / 2;
            if (itens[pai].Peso <= itens[i].Peso) break;

            (itens[pai], itens[i]) = (itens[i], itens[pai]);
            i = pai;
        }
    }

    private void Descer(int i)
    {
        while (true)
        {
            var menor = i;
            var esquerda = 2 * i + 1;
            var direita = 2 * i + 2;

            if (esquerda < Quantos && itens[esquerda].Peso < itens[menor].Peso) menor = esquerda;
            if (direita < Quantos && itens[direita].Peso < itens[menor].Peso) menor = direita;

            if (menor == i) break;

            (itens[menor], itens[i]) = (itens[i], itens[menor]);
            i = menor;
        }
    }
}
