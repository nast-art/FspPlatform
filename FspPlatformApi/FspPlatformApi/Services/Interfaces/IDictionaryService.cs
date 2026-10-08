using FspPlatformApi.DTOs.Dictionaries;

namespace FspPlatformApi.Services.Interfaces;

public interface IDictionaryService
{
    Task<DictionaryDtos> GetAllAsync();
}