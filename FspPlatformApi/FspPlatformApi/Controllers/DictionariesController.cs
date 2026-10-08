using FspPlatformApi.DTOs.Dictionaries;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[ApiController]
[Route("api/dictionaries")]
public class DictionariesController : ControllerBase
{
    private readonly IDictionaryService _svc;
    public DictionariesController(IDictionaryService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<DictionaryDtos>> GetAll() => Ok(await _svc.GetAllAsync());
}