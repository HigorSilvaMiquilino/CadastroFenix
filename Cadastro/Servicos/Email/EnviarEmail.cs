using Cadastro.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net.Sockets;

namespace Cadastro.Servicos.Email
{
    public class EnviarEmail : IEmailSender
    {
        private readonly CadastroContexto _contexto;
        private readonly IConfiguration _config;

        public EnviarEmail(CadastroContexto contexto, IConfiguration config)
        {
            _contexto = contexto;
            _config = config;
        }

        public async Task EnviarEmailslAsync(string email, string nome, string assunto, string mensagem, string imagemUrl)
        {
            var fromAddress = _config["EmailSettings:DefaultEmailAddress"];
            var smtpServer = _config["EmailSettings:Server"];
            var smtpPort = Convert.ToInt32(_config["EmailSettings:Port"]);
            var appPassword = _config["EmailSettings:Password"];

            var corpoEmail = await CarregarTemplateAsync(email, nome, mensagem, imagemUrl);

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = assunto,
                Body = corpoEmail,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email));

            using var cliente = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new System.Net.NetworkCredential(fromAddress, appPassword),
                EnableSsl = true
            };

            try
            {
                await cliente.SendMailAsync(message);
            }
            catch (SocketException ex)
            {
                await LogEmailAsync(email,
                    EmailStatusEnum.Erro.ToString(),
                    "Falha ao se conectar ao SMTP server. Por favor verifique as configurações do SMTP.", ex.StackTrace);
                throw new SmtpException("Falha ao se conectar ao SMTP server. Por favor verifique as configurações do SMTP.", ex);
            }
            catch (SmtpException ex)
            {
                await LogEmailAsync(email,
                     EmailStatusEnum.Erro.ToString(),
                     "Falha ao mandar o e-mail.", ex.StackTrace);
                throw new SmtpException("Falha ao mandar o e-mail.", ex);
            }
            catch (Exception ex)
            {
                await LogEmailAsync(email,
                        EmailStatusEnum.Erro.ToString(),
                        "Um erro ocorreu enquanto eviava o e-mail.", ex.StackTrace);
                throw new Exception("Um erro ocorreu enquanto eviava o e-mail.", ex);
            }
        }

        public async Task<string> CarregarTemplateAsync(string email, string nome, string mensagem, string imagemUrl)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "html", "templates", "EmailTemplate.html");
            var emailTemplate = await File.ReadAllTextAsync(templatePath);

            var templatePersonalizado = emailTemplate
                .Replace("{Nome}", nome)
                .Replace("{Mensagem}", mensagem)
                .Replace("{ImagemPromocao}", imagemUrl);

            return templatePersonalizado;
        }

        public async Task LogEmailAsync(string email, string status, string mensagemErro, string stackTrace)
        {

            var logErro = new LogErro
            {
                DataErro = DateTime.Now,
                MensagemErro = mensagemErro,
                StackTrace = stackTrace,
                Status = EmailStatusEnum.Erro.ToString(),
            };

            try
            {
                _contexto.LogsErro.Add(logErro);
                await _contexto.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Erro ao salvar log de erro: {exception.Message}");
            }

        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }
    }
}
