using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class PrintStationService : IPrintStationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PrintStationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PrintStationDto>> GetAllPrintStationsAsync()
    {
        var stations = await _unitOfWork.PrintStations.GetAllAsync();
        return _mapper.Map<IEnumerable<PrintStationDto>>(stations);
    }

    public async Task<PrintStationDto?> GetPrintStationByIdAsync(int id)
    {
        var station = await _unitOfWork.PrintStations.GetFirstOrDefaultAsync(s => s.Id == id);
        return _mapper.Map<PrintStationDto?>(station);
    }

    public async Task<PrintStationDto> CreatePrintStationAsync(PrintStationCreateDto printStationCreateDto)
    {
        var station = _mapper.Map<PrintStation>(printStationCreateDto);
        await _unitOfWork.PrintStations.AddAsync(station);
        await _unitOfWork.SaveAsync();
        return _mapper.Map<PrintStationDto>(station);
    }

    public async Task<bool> UpdatePrintStationAsync(PrintStationUpdateDto printStationUpdateDto)
    {
        var station = await _unitOfWork.PrintStations.GetFirstOrDefaultAsync(s => s.Id == printStationUpdateDto.Id, tracked: false);
        if (station == null)
        {
            return false;
        }

        _mapper.Map(printStationUpdateDto, station);
        _unitOfWork.PrintStations.Update(station);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeletePrintStationAsync(int id)
    {
        var station = await _unitOfWork.PrintStations.GetFirstOrDefaultAsync(s => s.Id == id);
        if (station == null)
        {
            return false;
        }

        _unitOfWork.PrintStations.Remove(station);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
