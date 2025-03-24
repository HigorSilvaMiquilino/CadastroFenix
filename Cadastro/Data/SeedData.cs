namespace Cadastro.Data
{
    public static class SeedData
    {
        public static List<Funcionarios> GetFuncionariosSeed()
        {
            return new List<Funcionarios>
            {
                new Funcionarios { Id = 1, CPF = "11400435013", NomeCompleto = "Ana Pereira", motivo = "Funcionário" },
                new Funcionarios { Id = 2, CPF = "76698650080", NomeCompleto = "Bruno Silva", motivo = "Funcionário" },
                new Funcionarios { Id = 3, CPF = "37452415094", NomeCompleto = "Carla Souza", motivo = "Funcionário" },
                new Funcionarios { Id = 4, CPF = "02997489016", NomeCompleto = "Diego Costa", motivo = "Funcionário" },
                new Funcionarios { Id = 5, CPF = "69710540084", NomeCompleto = "Elena Santos", motivo = "Funcionário" },
                new Funcionarios { Id = 6, CPF = "74916864000", NomeCompleto = "Fábio Lima", motivo = "Funcionário" },
                new Funcionarios { Id = 7, CPF = "71483624072", NomeCompleto = "Gisele Almeida", motivo = "Funcionário" },
                new Funcionarios { Id = 8, CPF = "79733835064", NomeCompleto = "Hugo Mendes", motivo = "Funcionário" },
                new Funcionarios { Id = 9, CPF = "41078925062", NomeCompleto = "Inês Oliveira", motivo = "Funcionário" },
                new Funcionarios { Id = 10, CPF = "10919399002", NomeCompleto = "João Rocha", motivo = "Funcionário" }
            };
        }
    }
}