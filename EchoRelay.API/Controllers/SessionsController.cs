using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Messages.ServerDB;
using EchoRelay.Core.Server.Services.ServerDB;
using EchoRelay.Core.Server.Storage;
using EchoRelay.Core.Server.Storage.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace EchoRelay.API.Controllers
{
    [Route("sessions/")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        static GameServerRegistry? Registry => ApiServer.Instance?.RelayServer.ServerDBService.Registry;

        [HttpGet]
        public IActionResult Get(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest("Invalid page number");
                }

                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var servers = Registry.RegisteredGameServersBySessionId.Keys;
                var skip = (pageNumber - 1) * pageSize;
                var page = servers.Skip(skip).Take(pageSize);
                return Ok(page.Select(x => x.ToString()).ToArray());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{sessionId}")]
        public IActionResult SessionGet(Guid sessionId)
        {
            try
            {
                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var server = Registry.GetGameServer(sessionId);
                if (server == null)
                {
                    return NotFound("Session not found");
                }

                var sessionInfo = new SessionInfo(server);
                return Ok(sessionInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{sessionId}")]
        public async Task<IActionResult> SessionPost(Guid sessionId)
        {
            try
            {
                if (Registry == null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError, "Registry is null");
                }

                var server = Registry.GetGameServer(sessionId);
                if (server == null)
                {
                    return NotFound("Session not found");
                }

                using var reader = new StreamReader(HttpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest("Invalid request body");
                }

                var sessionInfo = JsonConvert.DeserializeObject<SessionInfo>(body);
                if (sessionInfo == null)
                {
                    return BadRequest("Invalid session info");
                }

                var lobbyType = (ERGameServerStartSession.LobbyType)sessionInfo.LobbyType;
                if (!Enum.IsDefined(typeof(ERGameServerStartSession.LobbyType), lobbyType))
                {
                    return BadRequest("Invalid lobby type");
                }

                var channel = Guid.Parse(sessionInfo.Channel);
                if (channel == Guid.Empty)
                {
                    return BadRequest("Invalid channel");
                }

                await server.StartSession(new XPlatformId(), lobbyType, channel, sessionInfo.GameType, sessionInfo.Level, new ERGameServerStartSession.SessionSettings
                {
                    AppId = "1369078409873402",
                    GameType = sessionInfo.GameType,
                    Level = sessionInfo.Level,
                });

                return Ok(server.SessionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
