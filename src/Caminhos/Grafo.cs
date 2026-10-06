namespace Conde.Caminhos;

/// <summary>
/// O SORTEIO, escrito aqui em vez de vir da biblioteca.
///
/// A documentacao do sorteio de biblioteca nao promete a mesma sequencia entre
/// versoes nem entre sistemas, e a integracao continua roda em tres. Uma medida
/// de "quantas vezes o Dijkstra erra com peso negativo" com grafos diferentes em
/// cada sistema nao e uma medida: sao tres medidas que ninguem pode comparar.
/// </summary>
public sealed class Sorteio
{
    private ulong estado;

    public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

    public uint Proximo()
    {
        estado = estado * 6364136223846793005UL + 1442695040888963407UL;
        return (uint)(estado >> 32);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    public int Entre(int menor, int maior) => menor + Ate(maior - menor + 1);

    public double Fracao() => Proximo() / 4294967296.0;
}

/// <summary>Uma aresta dirigida, com peso inteiro.</summary>
public readonly record struct Aresta(int De, int Para, long Peso);

/// <summary>
/// Um GRAFO DIRIGIDO com pesos inteiros.
///
/// Os pesos sao inteiros de proposito, e nao numeros com virgula. Caminho minimo
/// e um problema em que a resposta e uma SOMA, e somar numero com virgula
/// introduz uma diferenca na ultima casa que faz dois caminhos empatados
/// desempatarem de um jeito em cada maquina. Com inteiro, o juiz e o algoritmo
/// chegam ao mesmo numero ou um dos dois esta errado.
/// </summary>
public sealed class Grafo
{
    private readonly List<Aresta>[] saindo;

    public Grafo(int vertices)
    {
        if (vertices <= 0) throw new ArgumentOutOfRangeException(nameof(vertices));

        Vertices = vertices;
        saindo = new List<Aresta>[vertices];
        for (var i = 0; i < vertices; i++) saindo[i] = [];
    }

    public int Vertices { get; }

    public int Arestas { get; private set; }

    public IReadOnlyList<Aresta> Saindo(int vertice) => saindo[vertice];

    public IEnumerable<Aresta> Todas => saindo.SelectMany(a => a);

    public bool TemNegativa => Todas.Any(a => a.Peso < 0);

    public void Ligar(int de, int para, long peso)
    {
        if (de < 0 || de >= Vertices) throw new ArgumentOutOfRangeException(nameof(de));
        if (para < 0 || para >= Vertices) throw new ArgumentOutOfRangeException(nameof(para));

        saindo[de].Add(new Aresta(de, para, peso));
        Arestas++;
    }

    /// <summary>
    /// O grafo com as arestas INVERTIDAS, que algumas medidas precisam.
    /// </summary>
    public Grafo Invertido()
    {
        var outro = new Grafo(Vertices);
        foreach (var a in Todas) outro.Ligar(a.Para, a.De, a.Peso);

        return outro;
    }

    /// <summary>
    /// Um grafo SORTEADO com pesos nao negativos.
    ///
    /// A densidade e a fracao dos pares possiveis que viram aresta, e ela importa
    /// mais do que parece: ela e o que decide qual algoritmo ganha.
    /// </summary>
    public static Grafo Sortear(int vertices, double densidade, ulong semente, long pesoMaximo = 100)
    {
        var sorteio = new Sorteio(semente);
        var grafo = new Grafo(vertices);

        for (var de = 0; de < vertices; de++)
            for (var para = 0; para < vertices; para++)
            {
                if (de == para) continue;
                if (sorteio.Fracao() >= densidade) continue;

                grafo.Ligar(de, para, sorteio.Entre(1, (int)pesoMaximo));
            }

        return grafo;
    }

    /// <summary>
    /// Um grafo sorteado com arestas NEGATIVAS e SEM ciclo negativo, por
    /// construcao.
    ///
    /// Garantir as duas coisas ao mesmo tempo nao e obvio, e a primeira versao
    /// daqui errava: ela so deixava negativas as arestas que iam de um indice
    /// menor para um maior, achando que as arestas de volta, grandes e positivas,
    /// pagariam a conta. Nao pagam: quatro arestas de -50 seguidas de uma de +100
    /// fecham um ciclo de -100, e o Bellman-Ford acusou em grafos de dez vertices.
    ///
    /// O jeito que FUNCIONA e o proprio truque de Johnson, de tras para a frente:
    /// comeca com pesos todos positivos (nenhum ciclo negativo, porque todo ciclo
    /// soma positivo) e aplica w(u,v) + h(u) - h(v) com potenciais h sorteados. Esse
    /// ajuste NAO MUDA o peso de nenhum ciclo, porque os potenciais do meio se
    /// cancelam e a ponta volta no fim, mas muda o peso de cada aresta e deixa
    /// muitas negativas.
    ///
    /// E a mesma identidade que o algoritmo de Johnson usa para o outro lado: la
    /// ela tira os negativos, aqui ela poe.
    /// </summary>
    public static Grafo SortearComNegativas(int vertices, double densidade, ulong semente, long pesoMaximo = 100)
    {
        var sorteio = new Sorteio(semente);
        var grafo = new Grafo(vertices);
        var potencial = new long[vertices];

        for (var v = 0; v < vertices; v++) potencial[v] = sorteio.Entre(0, (int)pesoMaximo);

        for (var de = 0; de < vertices; de++)
            for (var para = 0; para < vertices; para++)
            {
                if (de == para) continue;
                if (sorteio.Fracao() >= densidade) continue;

                var peso = sorteio.Entre(1, (int)pesoMaximo);
                grafo.Ligar(de, para, peso + potencial[de] - potencial[para]);
            }

        return grafo;
    }

    /// <summary>
    /// Um grafo com CICLO NEGATIVO, de proposito.
    ///
    /// Ele existe para a medida mostrar o que cada algoritmo faz quando a pergunta
    /// nao tem resposta: um detecta, outro devolve um numero.
    /// </summary>
    public static Grafo ComCicloNegativo(int vertices = 5)
    {
        var grafo = new Grafo(vertices);

        for (var i = 0; i < vertices - 1; i++) grafo.Ligar(i, i + 1, 1);

        // o ciclo: 1 -> 2 -> 3 -> 1 somando -2
        grafo.Ligar(2, 1, -4);
        grafo.Ligar(1, 2, 1);

        return grafo;
    }

    /// <summary>
    /// Uma GRADE, que e o grafo em que a busca com heuristica faz sentido: os
    /// vertices tem posicao e da para estimar a distancia que falta.
    /// </summary>
    public static Grafo Grade(int lado, double paredes, ulong semente, out bool[] livre)
    {
        var sorteio = new Sorteio(semente);
        var n = lado * lado;
        var passavel = new bool[n];

        for (var i = 0; i < n; i++) passavel[i] = sorteio.Fracao() >= paredes;

        // o comeco e o fim nunca sao parede
        passavel[0] = true;
        passavel[n - 1] = true;

        var grafo = new Grafo(n);

        for (var linha = 0; linha < lado; linha++)
            for (var coluna = 0; coluna < lado; coluna++)
            {
                var aqui = linha * lado + coluna;
                if (!passavel[aqui]) continue;

                foreach (var (dl, dc) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    var l = linha + dl;
                    var c = coluna + dc;

                    if (l < 0 || l >= lado || c < 0 || c >= lado) continue;

                    var la = l * lado + c;
                    if (passavel[la]) grafo.Ligar(aqui, la, 1);
                }
            }

        livre = passavel;

        return grafo;
    }
}
