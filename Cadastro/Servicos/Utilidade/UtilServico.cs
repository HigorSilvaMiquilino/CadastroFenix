using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Cadastro.Servicos.Utilidade
{
    public class UtilServico : IUtilServico
    {
        public string FormatarNomeCompleto(string nomeCompleto)
        {
            if(string.IsNullOrWhiteSpace(nomeCompleto))
                return string.Empty;

            return nomeCompleto.ToUpper().Trim();
        }

        public string FormatarCPF(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;
            return cpf.Replace(".", "").Replace("-", "").Trim();
        }

        public string FormatarDataNascimento(string dataNascimento)
        {
            if (string.IsNullOrWhiteSpace(dataNascimento))
                throw new ArgumentException("Data de nascimento não pode ser vazia.");

            try
            {
                var date = DateTime.ParseExact(dataNascimento, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                return date.ToString("yyyy-MM-dd");
            }
            catch (FormatException)
            {
                throw new FormatException($"A data '{dataNascimento}' não está no formato esperado 'dd/MM/yyyy'.");
            }
        }

        public string FormatarTimestamp(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public string FormatarGenero(string genero)
        {
            if (string.IsNullOrWhiteSpace(genero))
                return string.Empty;
            return genero.Substring(0, 1).ToUpper();
        }

        public string FormatarTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return string.Empty;
            return telefone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "").Trim();
        }

        public string FormatarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;
            return email.Trim().ToLower();
        }

        public string FormatarCEP(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
                return string.Empty;
            return cep.Replace("-", "").Trim();
        }

        public string FormatarLogradouro(string logradouro)
        {
            if (string.IsNullOrWhiteSpace(logradouro))
                return string.Empty;
            return logradouro.Trim().ToUpper();
        }

        public string FormatarBairro(string bairro)
        {
            if (string.IsNullOrWhiteSpace(bairro))
                return string.Empty;
            return bairro.Trim().ToUpper();
        }

        public string FormatarCidade(string cidade)
        {
            if (string.IsNullOrWhiteSpace(cidade))
                return string.Empty;
            return cidade.Trim().ToUpper();
        }

    }
}
