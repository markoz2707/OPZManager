using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPZManager.API.DTOs.Admin;
using OPZManager.API.DTOs.Common;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/training-data")]
    [Authorize(Roles = "Admin")]
    public class TrainingDataController : ControllerBase
    {
        private readonly ITrainingDataService _trainingDataService;
        private readonly IMapper _mapper;

        public TrainingDataController(ITrainingDataService trainingDataService, IMapper mapper)
        {
            _trainingDataService = trainingDataService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<TrainingDataDto>>> GetTrainingData([FromQuery] string? dataType = null)
        {
            var data = await _trainingDataService.GetTrainingDataAsync(dataType);
            return Ok(_mapper.Map<List<TrainingDataDto>>(data));
        }

        [HttpPost]
        public async Task<ActionResult<TrainingDataDto>> CreateTrainingData([FromBody] CreateTrainingDataDto dto)
        {
            var data = await _trainingDataService.CreateTrainingDataAsync(
                dto.Question, dto.Answer, dto.Context, dto.DataType);
            return CreatedAtAction(nameof(GetTrainingData), _mapper.Map<TrainingDataDto>(data));
        }

        [HttpPost("generate")]
        public async Task<ActionResult<List<TrainingDataDto>>> GenerateTrainingData()
        {
            var data = await _trainingDataService.GenerateTrainingDataAsync();
            return Ok(_mapper.Map<List<TrainingDataDto>>(data));
        }

        [HttpGet("export")]
        public async Task<ActionResult<object>> ExportTrainingData([FromQuery] string? dataType = null)
        {
            var json = await _trainingDataService.ExportTrainingDataAsJsonAsync(dataType);
            return Ok(new { data = json });
        }

        [HttpPost("import")]
        public async Task<ActionResult<object>> ImportTrainingData([FromBody] ImportTrainingDataDto dto)
        {
            var result = await _trainingDataService.ImportTrainingDataFromJsonAsync(dto.JsonData);
            if (!result)
                return BadRequest(new { message = "Import danych treningowych nie powiódł się." });

            return Ok(new { message = "Dane treningowe zostały zaimportowane pomyślnie." });
        }
    }

    // Small DTO kept near controller since only used here
    public class ImportTrainingDataDto
    {
        public string JsonData { get; set; } = string.Empty;
    }
}
