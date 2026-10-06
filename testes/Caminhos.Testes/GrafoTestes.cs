using Conde.Caminhos;
using Xunit;

namespace Conde.Caminhos.Testes;

public class GrafoTestes
{
    [Fact]
    public void OSorteioEhDeterminista()
    {
        var a = new Sorteio(7);
        var b = new Sorteio(7);

        for (var i = 0; i < 1000; i++) Assert.Equal(a.Proximo(), b.Proximo());
    }

    /// <summary>
    /// A fracao precisa cobrir os DOIS LADOS do meio: um deslocamento de 33 em vez
    /// de 32 deixaria o gerador com 31 bits, e um grafo com densidade acima da
    /// metade viraria o grafo completo. Os numeros continuariam plausiveis.
    /// </summary>
    [Fact]
    public void AFracaoCobreOsDoisLadosDoMeio()
    {
        var sorteio = new Sorteio(7);
        var acima = 0;
        var maior = 0.0;

        for (var i = 0; i < 10_000; i++)
        {
            var f = sorteio.Fracao();

            Assert.InRange(f, 0.0, 1.0);
            if (f > 0.5) acima++;
            if (f > maior) maior = f;
        }

        Assert.InRange(acima, 4_700, 5_300);
        Assert.True(maior > 0.999, $"o maior sorteio foi {maior}");
    }

    [Fact]
    public void EntreFicaDentroDoIntervalo()
    {
        var sorteio = new Sorteio(3);
        var viuOMenor = false;
        var viuOMaior = false;

        for (var i = 0; i < 5_000; i++)
        {
            var v = sorteio.Entre(-5, 7);

            Assert.InRange(v, -5, 7);
            if (v == -5) viuOMenor = true;
            if (v == 7) viuOMaior = true;
        }

        Assert.True(viuOMenor && viuOMaior, "as pontas do intervalo nunca sairam");
    }

    [Fact]
    public void AMesmaSementeDaOMesmoGrafo()
    {
        var um = Grafo.Sortear(20, 0.3, 9);
        var outro = Grafo.Sortear(20, 0.3, 9);

        Assert.Equal(um.Arestas, outro.Arestas);
        Assert.Equal(um.Todas.ToArray(), outro.Todas.ToArray());
    }

    /// <summary>
    /// A densidade pedida e a densidade entregue: sem isso, a tabela de
    /// cruzamento entre Floyd e Johnson estaria comparando outra coisa.
    /// </summary>
    [Fact]
    public void ADensidadePedidaEhADensidadeEntregue()
    {
        foreach (var densidade in new[] { 0.1, 0.3, 0.6, 1.0 })
        {
            var grafo = Grafo.Sortear(80, densidade, 5);
            var possiveis = 80 * 79;

            Assert.InRange((double)grafo.Arestas / possiveis, densidade - 0.03, densidade + 0.03);
        }
    }

    [Fact]
    public void OGrafoSorteadoComumNaoTemPesoNegativo()
    {
        for (ulong semente = 1; semente <= 20; semente++)
        {
            var grafo = Grafo.Sortear(30, 0.3, semente);

            Assert.False(grafo.TemNegativa);
            Assert.All(grafo.Todas, a => Assert.True(a.Peso >= 1));
        }
    }

    /// <summary>
    /// O grafo com negativas tem negativas e NAO tem ciclo negativo. A segunda
    /// parte e a dificil, e sem ela a medida nao existiria: com ciclo negativo a
    /// pergunta "qual o caminho minimo" nao tem resposta, e nao daria para
    /// comparar algoritmo nenhum.
    /// </summary>
    [Fact]
    public void OGrafoComNegativasNaoTemCicloNegativo()
    {
        var comNegativa = 0;

        for (ulong semente = 1; semente <= 100; semente++)
        {
            var grafo = Grafo.SortearComNegativas(10, 0.5, semente);

            if (grafo.TemNegativa) comNegativa++;

            for (var origem = 0; origem < grafo.Vertices; origem++)
                Assert.False(BellmanFord.De(grafo, origem).TemCicloNegativo, $"semente {semente}");
        }

        Assert.True(comNegativa > 90, $"so {comNegativa} de 100 grafos tinham peso negativo");
    }

    [Fact]
    public void OGrafoComCicloNegativoTemCicloNegativo()
    {
        var grafo = Grafo.ComCicloNegativo();

        Assert.True(BellmanFord.De(grafo, 0).TemCicloNegativo);
        Assert.True(TodosOsPares.Floyd(grafo).TemCicloNegativo);
    }

    [Fact]
    public void OInvertidoTemAsMesmasArestasAoContrario()
    {
        var grafo = Grafo.Sortear(15, 0.3, 4);
        var invertido = grafo.Invertido();

        Assert.Equal(grafo.Arestas, invertido.Arestas);

        foreach (var a in grafo.Todas)
            Assert.Contains(invertido.Saindo(a.Para), b => b.Para == a.De && b.Peso == a.Peso);
    }

    /// <summary>
    /// A grade e simetrica: toda passagem de A para B tem a volta de B para A, e e
    /// isso que faz a heuristica de quarteirao valer nos dois sentidos.
    /// </summary>
    [Fact]
    public void AGradeEhSimetricaEOComecoEOFimNaoSaoParede()
    {
        var grafo = Grafo.Grade(20, 0.3, 7, out var livre);

        Assert.True(livre[0]);
        Assert.True(livre[^1]);

        foreach (var a in grafo.Todas)
        {
            Assert.Equal(1, a.Peso);
            Assert.Contains(grafo.Saindo(a.Para), b => b.Para == a.De);
        }
    }

    [Fact]
    public void VerticeForaDaFaixaEhRecusado()
    {
        var grafo = new Grafo(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => grafo.Ligar(-1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => grafo.Ligar(0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grafo(0));
    }
}
