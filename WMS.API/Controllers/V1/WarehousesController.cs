namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WarehousesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command)
        {
            var warehouseId = await _mediator.Send(command);
            return Created("", new { Id = warehouseId });
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<WarehouseListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWarehouses([FromQuery] ListWarehousesQuery query)
        {
            var warehouses = await _mediator.Send(query);
            return Ok(warehouses);
        }
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWarehouseById([FromRoute] int id)
        {
            var query = new GetWarehouseByIdQuery { Id = id };
            var warehouse = await _mediator.Send(query);

            return Ok(warehouse);
        }
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateWarehouse([FromRoute] int id, [FromBody] UpdateWarehouseCommand command)
        {
            command.Id = id;
           
            await _mediator.Send(command);
            return NoContent();
        }
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateWarehouse([FromRoute] int id)
        {
            var command = new DeactivateWarehouseCommand { Id = id };
            await _mediator.Send(command);
            return NoContent();
        }
    }
}