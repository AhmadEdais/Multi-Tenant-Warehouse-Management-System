namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
public class CustomersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanManageCustomers)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var customerId = await sender.Send(command);
        return Created("", new { Id = customerId });
    }
    [HttpPut("{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageCustomers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCustomer([FromRoute] int id, [FromBody] UpdateCustomerCommand command)
    {
        var commandWithId = command with { Id = id };
        await sender.Send(commandWithId);
        return NoContent();
    }
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = SecurityPolicies.CanManageCustomers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateCustomer([FromRoute] int id)
    {
        await sender.Send(new DeactivateCustomerCommand(id));
        return NoContent();
    }
    [HttpGet("{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanViewCustomers)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCustomerById([FromRoute] int id)
    {
        var customer = await sender.Send(new GetCustomerByIdQuery(id));
        return Ok(customer);
    }
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanViewCustomers)]
    [ProducesResponseType(typeof(PagedResult<CustomerListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCustomers([FromQuery] ListCustomersQuery query)
    {
        var customers = await sender.Send(query);
        return Ok(customers);
    }
}