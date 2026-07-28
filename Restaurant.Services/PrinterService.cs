using AutoMapper;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Services;

public class PrinterService : IPrinterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PrinterService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PrinterDto>> GetAllPrintersAsync()
    {
        var printers = await _unitOfWork.Printers.GetAllAsync(includeProperties: "PrintStation");
        return _mapper.Map<IEnumerable<PrinterDto>>(printers);
    }

    public async Task<PrinterDto?> GetPrinterByIdAsync(int id)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == id, includeProperties: "PrintStation");
        return _mapper.Map<PrinterDto?>(printer);
    }

    public async Task<PrinterDto> CreatePrinterAsync(PrinterCreateDto printerCreateDto)
    {
        var printer = _mapper.Map<Printer>(printerCreateDto);
        await _unitOfWork.Printers.AddAsync(printer);
        await _unitOfWork.SaveAsync();
        
        var savedPrinter = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == printer.Id, includeProperties: "PrintStation");
        return _mapper.Map<PrinterDto>(savedPrinter);
    }

    public async Task<bool> UpdatePrinterAsync(PrinterUpdateDto printerUpdateDto)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == printerUpdateDto.Id, tracked: false);
        if (printer == null)
        {
            return false;
        }

        _mapper.Map(printerUpdateDto, printer);
        _unitOfWork.Printers.Update(printer);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }

    public async Task<bool> DeletePrinterAsync(int id)
    {
        var printer = await _unitOfWork.Printers.GetFirstOrDefaultAsync(p => p.Id == id);
        if (printer == null)
        {
            return false;
        }

        _unitOfWork.Printers.Remove(printer);
        var result = await _unitOfWork.SaveAsync();
        return result > 0;
    }
}
