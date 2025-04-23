using Cadastro.Data;
using Cadastro.DTO;

namespace Cadastro.Servicos.Auth
{
    public interface IAuthServico
    {
        Task<AuthResult> AutenticaAsync(string email, string senha);
        string GegarJwtToken(Usuario usuario);
        Task<PasswordResetToken> GerarTokenRecuperacaoSenha(Usuario usuario);
        Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> ValidarTurnstileToken(string token);
        Task<bool> VerificarEmailECPFexiste(string email, string cpf);
    }
}
