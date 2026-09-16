using WMS.Application.Features.Products.Queries;

namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var productId = await sender.Send(command);

        return Created(string.Empty, new { Id = productId });
    }
    [HttpPost("{id}/categories")]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignCategories(int id, [FromBody] List<int> categoryIds)
    {
        var command = new AssignProductCategoriesCommand(id, categoryIds);

        await sender.Send(command);

        return NoContent(); 
    }
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanViewCatalog)]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListProducts([FromQuery] ListProductsQuery query)
    {
        var result = await sender.Send(query);
        return Ok(result);
    }
}
