using Restaurant.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services.IServices;

public interface IRestaurantService
{
    Task<IEnumerable<RestaurantDto>> GetAllRestaurantsAsync();
    Task<RestaurantDto?> GetRestaurantByIdAsync(int id);
    Task<RestaurantDto> CreateRestaurantAsync(RestaurantCreateDto restaurantCreateDto, string? userId = null);
    Task<bool> UpdateRestaurantAsync(RestaurantUpdateDto restaurantUpdateDto);
    Task<bool> DeleteRestaurantAsync(int id);
}
