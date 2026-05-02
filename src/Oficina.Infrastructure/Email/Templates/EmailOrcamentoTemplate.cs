namespace Oficina.Infrastructure.Email.Templates;

public static class EmailOrcamentoTemplate
{
    public static string CriarHtml(string linkAprovar, string linkRecusar)
        => $"""
           <html>
             <body style="font-family: Arial, sans-serif; color: #1f2937;">
               <h2>Seu orçamento está aguardando decisão</h2>
               <p>Escolha uma ação para o orçamento do seu veículo.</p>
               <div style="margin-top: 20px;">
                 <a href="{linkAprovar}" style="background-color:#16a34a;color:white;padding:10px 16px;text-decoration:none;border-radius:6px;margin-right:8px;">Aprovar</a>
                 <a href="{linkRecusar}" style="background-color:#dc2626;color:white;padding:10px 16px;text-decoration:none;border-radius:6px;">Recusar</a>
               </div>
             </body>
           </html>
           """;
}
