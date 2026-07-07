using Xunit;

// Os testes de integração sobem um Kestrel real na porta 80 (Program.cs usa UseUrls fixo).
// Rodar classes de teste em paralelo faz múltiplas instâncias de WebApplicationFactory
// disputarem a mesma porta, causando respostas corrompidas / erros de parse JSON.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
