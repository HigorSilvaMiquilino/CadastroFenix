﻿using Cadastro.Data;
using Cadastro.DTO;
using Cadastro.Migrations;
using Cadastro.Servicos.Utilidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Identity.Client;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cadastro.Servicos.Cadastro
{
    public class CadastroServico : ICadastroServico
    {
        private readonly CadastroContexto _contexto;
        private readonly ILogger<CadastroServico> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string[] _generosValidos = { "Feminino", "Masculino", "Outro", "Prefiro não responder" };
        private readonly string[] _dddsValidos = {
            "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "21", "22", "24", "27", "28",
            "31", "32", "33", "34", "35", "37", "38",
            "41", "42", "43", "44", "45", "46", "47", "48", "49",
            "51", "53", "54", "55",
            "61", "62", "63", "64", "65", "66", "67", "68", "69",
            "71", "73", "74", "75", "77", "79",
            "81", "82", "83", "84", "85", "86", "87", "88", "89",
            "91", "92", "93", "94", "95", "96", "97", "98", "99"
        };


        public CadastroServico(CadastroContexto contexto, IHttpClientFactory httpClientFactory, ILogger<CadastroServico> logger)
        {
            _contexto = contexto;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> ehCPFFuncionario(string cpf, IDistributedCache cache)
        {
            cpf = cpf.Replace(".", "").Replace("-", "");
            cpf = new string(cpf?.Where(char.IsDigit).ToArray()) ?? throw new ArgumentNullException(nameof(cpf));

            var cacheKey = $"funcionario_unico_{cpf}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for cpf: {cpf}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var existe = await _contexto.Funcionarios.AnyAsync(f => f.CPF == cpf);

            await cache.SetStringAsync(cacheKey, (!existe).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for cpf: {cpf}, queried database");

            return !existe;
        }
        

        public async Task<bool> ehCPFUnico(string cpf, IDistributedCache cache)
        {
            cpf = cpf.Replace(".", "").Replace("-", "");
            cpf = new string(cpf?.Where(char.IsDigit).ToArray()) ?? throw new ArgumentNullException(nameof(cpf));


            var cacheKey = $"cpf_unico_{cpf}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for cpf: {cpf}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var exists = await _contexto.Usuarios.AnyAsync(u => u.CPF == cpf);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for CPF: {cpf}, queried database");
            return !exists;
        }

        public async Task<bool> ehCPFUnicoAsync(string cpf, IDistributedCache cache)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;
            cpf = cpf.Replace(".", "").Replace("-", "");
            cpf = new string(cpf?.Where(char.IsDigit).ToArray()) ?? throw new ArgumentNullException(nameof(cpf));

            var cacheKey = $"cpf_unico_{cpf}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for cpf: {cpf}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }
            
            var exists = await _contexto.Usuarios.AnyAsync(u => u.CPF == cpf);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for CPF: {cpf}, queried database");
            return !exists;
        }

        public bool ehCpfValido(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;
            tempCpf = cpf.Substring(0, 9);
            soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cpf.EndsWith(digito);
        }

        public bool ehNomeCompletoValido(string nomeCompleto)
        {
            if (string.IsNullOrWhiteSpace(nomeCompleto)) return false;
            var partes = nomeCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2 || partes.Any(p => p.Length < 2)) return false;
            return partes.All(p => p.All(char.IsLetter));
        }

        public bool ehDataNascimentoValida(string dataNascimento)
        {
            if (!DateTime.TryParseExact(dataNascimento, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var data))
                return false;
            return data <= DateTime.UtcNow.AddYears(-18);
        }

        public bool ehGeneroValido(string genero)
        {
            return !string.IsNullOrWhiteSpace(genero) && _generosValidos.Contains(genero);
        }

        public bool ehTelefoneValido(string telefone) 
        {
            if (string.IsNullOrWhiteSpace(telefone)) return false;

            telefone = telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");

            if (telefone.Length != 11 || !telefone.All(char.IsDigit)) return false;

            string ddd = telefone.Substring(0, 2);
            string numero = telefone.Substring(2);

            if (!_dddsValidos.Contains(ddd)) return false;
            return numero.StartsWith("9") && numero.Length == 9;
        }

        public async Task<bool> ehTelefoneUnico(string telefone, IDistributedCache cache)
        {
            telefone = telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            telefone = new string(telefone?.Where(char.IsDigit).ToArray()) ?? throw new ArgumentNullException(nameof(telefone));

            var cacheKey = $"telefone_unico_{telefone}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for telefone: {telefone}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var exists = await _contexto.Usuarios.AnyAsync(u => u.Telefone == telefone);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for telefone: {telefone}, queried database");
            return !exists;
        }

        public async Task<bool> ehTelefoneUnicoAsync(string telefone, IDistributedCache cache)
        {
            telefone = telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            telefone = new string(telefone?.Where(char.IsDigit).ToArray()) ?? throw new ArgumentNullException(nameof(telefone));

            var cacheKey = $"telefone_unico_{telefone}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for telefone: {telefone}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var exists = await _contexto.Usuarios.AnyAsync(u => u.Telefone == telefone);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for telefone: {telefone}, queried database");
            return !exists;
        }

        public async Task<bool> ehCepValido(string cep, string estado, string cidade, string bairro, string logradouro) 
        {
            if (string.IsNullOrWhiteSpace(cep) || !System.Text.RegularExpressions.Regex.IsMatch(cep, @"^\d{5}-\d{3}$"))
                return false;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://viacep.com.br/ws/{cep.Replace("-", "")}/json/");
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                var viaCepData = JsonSerializer.Deserialize<ViaCepResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (viaCepData == null) return false;

                if (viaCepData.Erro.HasValue && viaCepData.Erro.Value) return false;

                return viaCepData.Uf == estado && 
                    viaCepData.Localidade == cidade && 
                    viaCepData.Bairro == bairro && 
                    viaCepData.Logradouro == logradouro;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ehEmailValido(string email, string confirmacaoEmail)
        {
            if(string.IsNullOrWhiteSpace(email) || email.Length > 254 || string.IsNullOrWhiteSpace(confirmacaoEmail) ) return false;

            // Ex: aa@a.aa
            var regex = new Regex(@"^[a-zA-Z0-9._-]{2,}@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

            if (!regex.IsMatch(email)) return false;

            if(email.Contains("..") || email.Contains("--") || email.Contains(".-") || email.Contains("-.") || email.Contains(".@") || email.Contains("@.") || email.Contains("-@") || email.Contains("@-")) return false;

            if(email.StartsWith(".") || email.StartsWith("-") || email.EndsWith(".") || email.EndsWith("-")) return false;  

            if(email.Count(c => c == '@') != 1) return false;

            return email.Equals(confirmacaoEmail);
        }

        public async Task<bool> ehEmailUnico(string email, IDistributedCache cache)
        {
            email = email?.ToLower() ?? throw new ArgumentNullException(nameof(email));

            var cacheKey = $"email_unico_{email}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for email: {email}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var exists = await _contexto.Usuarios.AnyAsync(u => u.Email == email);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for email: {email}, queried database");

            return !exists;
        }

        public async Task<bool> ehEmailUnicoAsync(string email, IDistributedCache cache)
        {
            email = email?.ToLower() ?? throw new ArgumentNullException(nameof(email));

            var cacheKey = $"email_unico_{email}";

            try
            {
                var cachedResult = await cache.GetStringAsync(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation($"Cache hit for email: {email}");
                    return bool.Parse(cachedResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to access Redis cache for key: {CacheKey}, falling back to database", cacheKey);
            }

            var exists = await _contexto.Usuarios.AnyAsync(u => u.Email == email);

            await cache.SetStringAsync(cacheKey, (!exists).ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            _logger.LogInformation($"Cache miss for email: {email}, queried database");

            return !exists;
        }

        public bool ehSenhaValida(string senha, string confirmacaoSenha)
        {
            if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(confirmacaoSenha) || senha.Length < 8 || senha.Length > 80) return false;
            var regex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9].*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$");
            if (!regex.IsMatch(senha) || !regex.IsMatch(confirmacaoSenha)) return false;
            return senha.Equals(confirmacaoSenha);
        }

        public bool ehAceiteTermosValido(bool aceiteTermos)
        {
            return aceiteTermos;
        }
    }
}
