using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPZManager.API.DTOs.Common;
using OPZManager.API.DTOs.Equipment;
using OPZManager.API.Exceptions;
using OPZManager.API.Models;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/equipment")]
    [Authorize]
    public class EquipmentController : ControllerBase
    {
        private readonly IEquipmentMatchingService _equipmentMatchingService;
        private readonly IMapper _mapper;

        public EquipmentController(IEquipmentMatchingService equipmentMatchingService, IMapper mapper)
        {
            _equipmentMatchingService = equipmentMatchingService;
            _mapper = mapper;
        }

        [HttpGet("manufacturers")]
        public async Task<ActionResult<List<ManufacturerDto>>> GetManufacturers()
        {
            var manufacturers = await _equipmentMatchingService.GetAllManufacturersAsync();
            return Ok(_mapper.Map<List<ManufacturerDto>>(manufacturers));
        }

        [HttpGet("types")]
        public async Task<ActionResult<List<EquipmentTypeDto>>> GetTypes()
        {
            var types = await _equipmentMatchingService.GetAllEquipmentTypesAsync();
            return Ok(_mapper.Map<List<EquipmentTypeDto>>(types));
        }

        [HttpGet("models")]
        public async Task<ActionResult<List<EquipmentModelDto>>> GetModels()
        {
            var models = await _equipmentMatchingService.GetAllEquipmentModelsAsync();
            return Ok(_mapper.Map<List<EquipmentModelDto>>(models));
        }

        [HttpGet("models/{id}")]
        public async Task<ActionResult<EquipmentModelDto>> GetModel(int id)
        {
            var model = await _equipmentMatchingService.GetEquipmentModelByIdAsync(id);
            if (model == null)
                throw new NotFoundException("Model", id);

            return Ok(_mapper.Map<EquipmentModelDto>(model));
        }

        [HttpPost("manufacturers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ManufacturerDto>> CreateManufacturer([FromBody] CreateManufacturerDto dto)
        {
            var manufacturer = _mapper.Map<Manufacturer>(dto);
            var created = await _equipmentMatchingService.CreateManufacturerAsync(manufacturer);
            return CreatedAtAction(nameof(GetManufacturers), new { id = created.Id }, _mapper.Map<ManufacturerDto>(created));
        }

        [HttpPost("types")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EquipmentTypeDto>> CreateType([FromBody] CreateEquipmentTypeDto dto)
        {
            var equipmentType = _mapper.Map<EquipmentType>(dto);
            var created = await _equipmentMatchingService.CreateEquipmentTypeAsync(equipmentType);
            return CreatedAtAction(nameof(GetTypes), new { id = created.Id }, _mapper.Map<EquipmentTypeDto>(created));
        }

        [HttpPost("models")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<EquipmentModelDto>> CreateModel([FromBody] CreateEquipmentModelDto dto)
        {
            var equipmentModel = _mapper.Map<EquipmentModel>(dto);
            var created = await _equipmentMatchingService.CreateEquipmentModelAsync(equipmentModel);
            // Reload with includes for the DTO mapping
            var loaded = await _equipmentMatchingService.GetEquipmentModelByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetModels), new { id = created.Id }, _mapper.Map<EquipmentModelDto>(loaded));
        }

        [HttpDelete("manufacturers/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> DeleteManufacturer(int id)
        {
            var result = await _equipmentMatchingService.DeleteManufacturerAsync(id);
            if (!result)
                throw new BusinessRuleException("Nie można usunąć producenta. Producent nie istnieje lub posiada przypisane modele sprzętu.");

            return Ok(new { message = "Producent został usunięty." });
        }

        [HttpDelete("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> DeleteType(int id)
        {
            var result = await _equipmentMatchingService.DeleteEquipmentTypeAsync(id);
            if (!result)
                throw new BusinessRuleException("Nie można usunąć typu sprzętu. Typ nie istnieje lub posiada przypisane modele.");

            return Ok(new { message = "Typ sprzętu został usunięty." });
        }

        [HttpDelete("models/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> DeleteModel(int id)
        {
            var result = await _equipmentMatchingService.DeleteEquipmentModelAsync(id);
            if (!result)
                throw new NotFoundException("Model", id);

            return Ok(new { message = "Model został usunięty." });
        }
    }
}
