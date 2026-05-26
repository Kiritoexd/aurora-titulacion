using MimeKit;
using System.Threading.Tasks;
using System.Linq;

namespace AURORA.Servicios
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "auroraappoficial@gmail.com";
        private readonly string _smtpPass = "qxqc jhbi ackt zafv";

        public async Task SendPasswordRecoveryCodeAsync(string toEmail, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AURORA App", _smtpUser));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Código de recuperación · AURORA";

            var codeBoxes = string.Join("", code.Select(c => $@"
                <td style=""padding:0 5px;"">
                  <div style=""
                    width:52px;
                    height:64px;
                    background:#1e1e1e;
                    border:1px solid rgba(200,169,110,0.35);
                    border-radius:12px;
                    font-size:28px;
                    font-weight:700;
                    color:#c8a96e;
                    text-align:center;
                    line-height:64px;
                    font-family:'DM Sans',Arial,sans-serif;
                    box-shadow:0 0 0 3px rgba(200,169,110,0.08);
                    letter-spacing:0;
                  "">{c}</div>
                </td>"));

            message.Body = new TextPart("html")
            {
                Text = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Código de recuperación · AURORA</title>
</head>
<body style=""margin:0;padding:0;background:#0d0d0d;font-family:'DM Sans',Arial,sans-serif;"">

  <!-- Wrapper -->
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
         style=""background:#0d0d0d;padding:48px 16px;"">
    <tr>
      <td align=""center"">

        <!-- Card -->
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""
               style=""max-width:520px;
                       background:#161616;
                       border-radius:20px;
                       border:1px solid rgba(255,255,255,0.08);
                       box-shadow:0 24px 64px rgba(0,0,0,0.7);
                       overflow:hidden;"">

          <!-- Header -->
          <tr>
            <td style=""background:#111111;
                        border-bottom:1px solid rgba(255,255,255,0.06);
                        padding:28px 40px;
                        text-align:center;"">

              <!-- Logo pill -->
              <div style=""display:inline-block;
                           background:rgba(200,169,110,0.12);
                           border:1px solid rgba(200,169,110,0.25);
                           border-radius:12px;
                           padding:8px 22px;
                           font-size:15px;
                           font-weight:700;
                           letter-spacing:3px;
                           color:#c8a96e;
                           text-transform:uppercase;"">
                AURORA
              </div>
            </td>
          </tr>

          <!-- Icon row -->
          <tr>
            <td align=""center"" style=""padding:36px 40px 0;"">
              <div style=""width:56px;
                           height:56px;
                           background:rgba(200,169,110,0.12);
                           border:1px solid rgba(200,169,110,0.25);
                           border-radius:16px;
                           display:inline-flex;
                           align-items:center;
                           justify-content:center;
                           font-size:26px;
                           line-height:56px;
                           text-align:center;"">
                
              </div>
            </td>
          </tr>

          <!-- Title -->
          <tr>
            <td align=""center"" style=""padding:20px 40px 0;"">
              <h1 style=""margin:0;
                          font-size:26px;
                          font-weight:400;
                          color:#ececec;
                          letter-spacing:-0.5px;
                          line-height:1.2;"">
                Recuperación de contraseña
              </h1>
              <!-- Gold accent line -->
              <div style=""width:36px;
                           height:2px;
                           background:linear-gradient(90deg,#c8a96e,transparent);
                           margin:12px auto 0;
                           border-radius:2px;""></div>
            </td>
          </tr>

          <!-- Body text -->
          <tr>
            <td align=""center"" style=""padding:20px 40px 0;"">
              <p style=""margin:0;
                         font-size:15px;
                         color:#888888;
                         line-height:1.6;"">
                Hola, recibimos una solicitud para restablecer tu contraseña.<br/>
                Usa el siguiente código para continuar:
              </p>
            </td>
          </tr>

          <!-- Code boxes -->
          <tr>
            <td align=""center"" style=""padding:32px 40px;"">
              <table cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>
                  {codeBoxes}
                </tr>
              </table>
            </td>
          </tr>

          <!-- Divider -->
          <tr>
            <td style=""padding:0 40px;"">
              <div style=""height:1px;background:rgba(255,255,255,0.06);""></div>
            </td>
          </tr>

          <!-- Warning text -->
          <tr>
            <td align=""center"" style=""padding:28px 40px;"">
              <table cellpadding=""0"" cellspacing=""0"" border=""0""
                     style=""background:rgba(200,169,110,0.06);
                             border:1px solid rgba(200,169,110,0.15);
                             border-radius:12px;
                             padding:16px 20px;
                             max-width:360px;"">
                <tr>
                  <td style=""font-size:13px;color:#888;line-height:1.7;text-align:center;"">
                    ⏱ &nbsp;Este código expira en <strong style=""color:#c8a96e;"">15 minutos</strong>.<br/>
                    Si no solicitaste este cambio, puedes ignorar este correo con seguridad.
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#111111;
                        border-top:1px solid rgba(255,255,255,0.06);
                        padding:20px 40px;
                        text-align:center;"">
              <p style=""margin:0;font-size:12px;color:#555555;line-height:1.6;"">
                © 2026 AURORA App &nbsp;·&nbsp; Todos los derechos reservados<br/>
                <span style=""font-size:11px;color:#3a3a3a;"">
                  Este es un correo automático, por favor no respondas a este mensaje.
                </span>
              </p>
            </td>
          </tr>

        </table>
        <!-- /Card -->

      </td>
    </tr>
  </table>
  <!-- /Wrapper -->

</body>
</html>"
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}