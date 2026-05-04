namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var categoryId = await sender.Send(command);

        return Created(string.Empty, new { Id = categoryId });
    }
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanViewCatalog)]
    [ProducesResponseType(typeof(PagedResult<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListCategories([FromQuery] ListCategoriesQuery query)
    {
        var result = await sender.Send(query);
        return Ok(result);
    }
    [HttpGet("tree")]
    [Authorize(Policy = SecurityPolicies.CanViewCatalog)]
    [ProducesResponseType(typeof(List<CategoryTreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoryTree([FromQuery] GetCategoryTreeQuery query)
    {
        var result = await sender.Send(query);
        return Ok(result);
    }
}