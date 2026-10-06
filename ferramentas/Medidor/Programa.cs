using Conde.Caminhos;

var pedido = args.Length > 0 ? args[0] : "tudo";
var tudo = pedido == "tudo";

if (tudo || pedido == "juiz") Juiz();
if (tudo || pedido == "negativo") Negativas();
if (tudo || pedido == "densidade") Densidades();
if (tudo || pedido == "estrela") Estrelas();
if (tudo || pedido == "ciclo") Ciclos();

static void Juiz()
{
    Console.WriteLine();
    Console.WriteLine("  O JUIZ: TODOS OS CAMINHOS SIMPLES, CONTRA OS DOIS ALGORITMOS");
    Console.WriteLine("  (grafos sorteados com peso negativo e sem ciclo negativo)");
    Console.WriteLine();
    Console.WriteLine("  vertices   grafos   caminhos olhados   dijkstra errou   bellman-ford errou");

    foreach (var l in Medidas.Juiz())
        Console.WriteLine($"  {l.Vertices,8} {l.Grafos,8} {l.Caminhos,18:N0} {l.DijkstraErrou,16:N0} {l.BellmanErrou,20:N0}");

    Console.WriteLine();
}

static void Negativas()
{
    Console.WriteLine();
    Console.WriteLine("  AS DUAS TENTATIVAS QUE NAO FUNCIONAM");
    Console.WriteLine();
    Console.WriteLine("  jeito              grafos   errou em   pior caso (vertices errados)");

    foreach (var l in Medidas.Negativas())
        Console.WriteLine($"  {l.Jeito,-18} {l.Grafos,7} {l.Errou,10:N0} {l.PiorErro,29:N0}");

    Console.WriteLine();
    Console.WriteLine("  O MENOR GRAFO EM QUE O DIJKSTRA ERRA: 4 VERTICES, 4 ARESTAS");
    Console.WriteLine();

    var menor = Negativos.MenorErroDoDijkstra();

    Console.WriteLine("    0 -> 1 com peso  2");
    Console.WriteLine("    0 -> 2 com peso  3");
    Console.WriteLine("    2 -> 1 com peso -2");
    Console.WriteLine("    1 -> 3 com peso  1");
    Console.WriteLine();
    Console.WriteLine($"    a resposta certa para o 3:     {ForcaBruta.De(menor, 0)[3]}");
    Console.WriteLine($"    o Dijkstra devolveu:           {Dijkstra.De(menor, 0).Distancia[3]}");
    Console.WriteLine($"    o Bellman-Ford:                {BellmanFord.De(menor, 0).Resultado.Distancia[3]}");
    Console.WriteLine();

    Console.WriteLine("  E SOMAR CONSTANTE ERRA ATE SEM PESO NEGATIVO");
    Console.WriteLine();

    var constante = Negativos.MenorErroDaConstante();

    Console.WriteLine("    0 -> 1 -> 2 -> 3, cada aresta com peso 1   (tres saltos, peso 3)");
    Console.WriteLine("    0 -> 3 com peso 4                          (um salto, peso 4)");
    Console.WriteLine();
    Console.WriteLine($"    a resposta certa:                 {ForcaBruta.De(constante, 0)[3]}");
    Console.WriteLine($"    somando 10 em cada aresta:        {Negativos.ComConstante(constante, 0, 10)[3]}");
    Console.WriteLine();
}

static void Densidades()
{
    Console.WriteLine();
    Console.WriteLine("  TODOS OS PARES: ONDE AS CURVAS SE CRUZAM (60 vertices)");
    Console.WriteLine();
    Console.WriteLine("  densidade   arestas        floyd      johnson   n dijkstras      vencedor");

    foreach (var l in Medidas.Densidades())
    {
        var vencedor = l.Floyd <= l.Johnson ? "floyd" : "johnson";
        Console.WriteLine($"  {l.Densidade,9:F2} {l.Arestas,9:N0} {l.Floyd,12:N0} {l.Johnson,12:N0} {l.NDijkstras,13:N0} {vencedor,13}");
    }

    Console.WriteLine();
    Console.WriteLine("  QUANTAS PASSADAS O BELLMAN-FORD REALMENTE PRECISA");
    Console.WriteLine();
    Console.WriteLine("  vertices   densidade   passadas usadas   passadas do livro");

    foreach (var l in Medidas.Passadas())
        Console.WriteLine($"  {l.Vertices,8} {l.Densidade,11:F2} {l.PassadasUsadas,17} {l.PassadasDoLivro,19}");

    Console.WriteLine();
}

static void Estrelas()
{
    Console.WriteLine();
    Console.WriteLine("  A BUSCA COM PALPITE NUMA GRADE DE 60 POR 60, COM 25 POR CENTO DE PAREDES");
    Console.WriteLine();
    Console.WriteLine("  heuristica             distancia   expandidos   reexpandidos   superestima em   inconsistente em");

    foreach (var l in Medidas.Estrelas())
        Console.WriteLine($"  {l.Heuristica,-22} {l.Distancia,9:N0} {l.Expandidos,12:N0} {l.Reexpandidos,14:N0} {l.Superestima,16:N0} {l.Inconsistente,18:N0}");

    Console.WriteLine();
    Console.WriteLine("  O PRECO DO EXAGERO");
    Console.WriteLine();
    Console.WriteLine("  fator   distancia   expandidos   erro");

    foreach (var l in Medidas.Exageros())
        Console.WriteLine($"  {l.Fator,5} {l.Distancia,11:N0} {l.Expandidos,12:N0} {l.ErroPorCento,6:F1} %");

    Console.WriteLine();
}

static void Ciclos()
{
    Console.WriteLine();
    Console.WriteLine("  QUANDO A PERGUNTA NAO TEM RESPOSTA (grafo com ciclo negativo)");
    Console.WriteLine();

    foreach (var l in Medidas.Ciclos())
        Console.WriteLine($"  {l.Algoritmo,-16} {l.Resposta}");

    Console.WriteLine();
}
