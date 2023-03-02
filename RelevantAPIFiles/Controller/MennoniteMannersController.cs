using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Game;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameType;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Participant;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Settings;

namespace TaiwoTech.Eltee.Controllers.Mennonite
{
    [ApiController]
    [Route("api/mennonite/[controller]")]
    public class MennoniteMannersController : Controller
    {
        private ILogger<MennoniteMannersController> Logger { get; }
        private GameService GameService { get; }
        public GameTypeService GameTypeService { get; }
        private ParticipantService ParticipantService { get; }
        private SettingsService SettingsService { get; }

        public MennoniteMannersController(
            ILogger<MennoniteMannersController> logger,
            GameService gameService,
            GameTypeService gameTypeService,
            ParticipantService participantService,
            SettingsService settingsService
        )
        {
            Logger = logger;
            GameService = gameService;
            GameTypeService = gameTypeService;
            ParticipantService = participantService;
            SettingsService = settingsService;
        }

        [HttpGet]
        [Route("settings")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MennoniteMannersGet))]
        public async Task<IActionResult> GetOverview()
        {
            Logger.LogDebug("Getting settings");
            var settingsDto= await SettingsService.Get();
            var gameTypes = await GameTypeService.Get();
            var overview = new MennoniteMannersGet(settingsDto, gameTypes);
            return Ok(overview);
        }

#if  DEBUG
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Test()
        {
            Logger.LogInformation("Creating a new Game");
            var gameDto = await GameService.Create();
            Logger.LogInformation("Created new game: {@gameDto}", gameDto);

            Logger.LogInformation("Adding three participants to {gameId}", gameDto.GameId);
            var newParticipantDto = new ParticipantDto
            {
                GameId = gameDto.GameId,
                UserName = "Eltee",
                ConnectionId = Guid.NewGuid().ToString(),
                IsHost = true,
                JoinedAt = DateTime.UtcNow,
                LeftAt = null
            };
            var createdParticipant1 = await ParticipantService.Add(newParticipantDto);
            Logger.LogInformation("{participant} added to game {gameId} as {uniqueParticipantName}", newParticipantDto.UserName, gameDto.GameId, createdParticipant1);
            newParticipantDto.IsHost = false;
            var createdParticipant2 = await ParticipantService.Add(newParticipantDto);
            Logger.LogInformation("{participant} added to game {gameId} as {uniqueParticipantName}", newParticipantDto.UserName, gameDto.GameId, createdParticipant2);
            var createdParticipant3 = await ParticipantService.Add(newParticipantDto);
            Logger.LogInformation("{participant} added to game {gameId} as {uniqueParticipantName}", newParticipantDto.UserName, gameDto.GameId, createdParticipant3);

            Logger.LogInformation("Removing {participantName} from game {gameId}", createdParticipant2, gameDto.GameId);
            var _ = await ParticipantService.Leave(gameId: gameDto.GameId, participantUniqueName: createdParticipant2);

            Logger.LogInformation("Getting list of participants from {gameId}", gameDto.GameId);
            var participants = await ParticipantService.Get(gameDto.GameId);

            var response = new
            {
                GameDto = gameDto,
                ParticipantsCreated = new []{createdParticipant1, createdParticipant2, createdParticipant3},
                Participants = participants
            };

            //Create a game
            //leave game

            //Create a game
            //Start Game
            //Leave game

            //Create a game
            //participant join
            //participant join
            //leave game
            //check new host
            
            //Create a game
            //participant join
            //participant join
            //start game
            //validate results


            return Ok(response);
        }

#endif
        }
}
