using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class RestaurantService : IRestaurantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RestaurantService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RestaurantDto>> GetAllRestaurantsAsync()
    {
        var restaurants = await _unitOfWork.Restaurants.GetAllAsync();
        return _mapper.Map<IEnumerable<RestaurantDto>>(restaurants);
    }

    public async Task<RestaurantDto?> GetRestaurantByIdAsync(int id)
    {
        var restaurant = await _unitOfWork.Restaurants.GetFirstOrDefaultAsync(r => r.Id == id);
        return _mapper.Map<RestaurantDto?>(restaurant);
    }

    public async Task<RestaurantDto> CreateRestaurantAsync(RestaurantCreateDto restaurantCreateDto, string? userId = null)
    {
        var restaurant = _mapper.Map<RestaurantInfo>(restaurantCreateDto);
        await _unitOfWork.Restaurants.AddAsync(restaurant);
        await _unitOfWork.SaveAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _unitOfWork.ApplicationUsers.GetFirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.RestaurantId = restaurant.Id;
                _unitOfWork.ApplicationUsers.Update(user);
                await _unitOfWork.SaveAsync();
            }
        }

        return _mapper.Map<RestaurantDto>(restaurant);
    }

    public async Task<bool> UpdateRestaurantAsync(RestaurantUpdateDto restaurantUpdateDto)
    {
        var restaurant = await _unitOfWork.Restaurants.GetFirstOrDefaultAsync(r => r.Id == restaurantUpdateDto.Id, tracked: false);
        if (restaurant == null)
        {
            return false;
        }

        _mapper.Map(restaurantUpdateDto, restaurant);
        _unitOfWork.Restaurants.Update(restaurant);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeleteRestaurantAsync(int id)
    {
        var restaurant = await _unitOfWork.Restaurants.GetFirstOrDefaultAsync(r => r.Id == id);
        if (restaurant == null)
        {
            return false;
        }

        _unitOfWork.Restaurants.Remove(restaurant);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
