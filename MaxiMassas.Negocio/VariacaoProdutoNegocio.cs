using AutoMapper;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Repositorio.Interfaces;

namespace MaxiMassas.Negocio;

public class VariacaoProdutoNegocio : IVariacaoProdutoNegocio
{
    private readonly IRepositorio<VariacaoProduto> _repo;
    private readonly IMapper _mapper;

    public VariacaoProdutoNegocio(IRepositorio<VariacaoProduto> repo, IMapper mapper)
    { _repo = repo; _mapper = mapper; }

    public async Task<IEnumerable<VariacaoProdutoResponse>> GetByProdutoIdAsync(int produtoId)
        => _mapper.Map<IEnumerable<VariacaoProdutoResponse>>(await _repo.FindAsync(v => v.ProdutoId == produtoId));

    public async Task<VariacaoProdutoResponse> CreateAsync(VariacaoProdutoRequest request)
    {
        var v = _mapper.Map<VariacaoProduto>(request);
        await _repo.AddAsync(v);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VariacaoProdutoResponse>(v);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v is null) return false;
        _repo.Remove(v);
        return await _repo.SaveChangesAsync();
    }
}