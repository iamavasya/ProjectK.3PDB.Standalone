using Microsoft.AspNetCore.Mvc;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.BL.Services;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;

namespace ProjectK._3PDB.Standalone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipantController : ControllerBase
    {
        private readonly ParticipantService _service;
        public ParticipantController(ParticipantService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ParticipantService>>> GetAll()
        {
            var response = await _service.GetAllAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Participant>> GetById(Guid participantKey)
        {
            var participant = await _service.GetByKeyAsync(participantKey);
            if (participant == null) return NotFound();
            return Ok(participant);
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не обрано або він порожній");

            if (!file.FileName.EndsWith(".csv"))
                return BadRequest("Підтримуються тільки .csv файли");

            try
            {
                using var stream = file.OpenReadStream();
                await _service.ImportCsvAsync(stream);
                return Ok(new { message = "Імпорт пройшов успішно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка імпорту: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid participantKey, [FromBody] ParticipantDto participant)
        {
            if (participantKey != participant.ParticipantKey)
                return BadRequest("ID в URL не співпадає з ID в тілі запиту");

            try
            {
                await _service.UpdateAsync(participant);
                return NoContent(); // 204 Success
            }
            catch (Exception ex) when (ex.Message == "Not found")
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<ParticipantHistory>>> GetHistory(Guid participantKey)
        {
            var history = await _service.GetHistoryAsync(participantKey);
            return Ok(history);
        }

        [HttpPost]
        public async Task<ActionResult<Participant>> Create([FromBody] ParticipantDto participant)
        {
            // Тут варто додати валідацію, якщо її немає в BL
            var created = await _service.CreateAsync(participant);
            return CreatedAtAction(nameof(GetById), new { id = created.ParticipantKey }, created);
        }
    }
}
