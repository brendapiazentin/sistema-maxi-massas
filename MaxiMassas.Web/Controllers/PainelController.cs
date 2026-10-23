using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MaxiMassas.Web.Seguranca;

namespace MaxiMassas.Web.Controllers;

/// <summary>
/// Base das telas do painel: todas exigem o login do painel (cookie). As telas de acesso
/// (Login, EsqueciSenha, RedefinirSenha) usam [AllowAnonymous].
/// </summary>
[Authorize(Policy = PainelAuth.Policy)]
public abstract class PainelController : Controller
{
}
