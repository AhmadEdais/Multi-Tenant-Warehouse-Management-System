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
    public async Task<IActionResult> GetCategoryTree()
    {
        var result = await sender.Send(new GetCategoryTreeQuery());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanViewCatalog)]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        return Ok(await sender.Send(new GetCategoryByIdQuery(id)));
    }

    [HttpPut("Update/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpPost("Deactivate/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCategory(int id)
    {
        await sender.Send(new DeactivateCategoryCommand(id));
        return NoContent();
    }

    [HttpPost("Reactivate/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageCategories)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateCategory(int id)
    {
        await sender.Send(new ReactivateCategoryCommand(id));
        return NoContent();
    }
}
