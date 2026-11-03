// Fotos dos produtos: confere e reduz a imagem enviada (JPEG, no maximo 800 px no lado maior) e guarda o arquivo.
// Com "Fotos:Bucket" configurado, grava no Amazon S3; no servidor a permissao vem da funcao do IAM ligada
// a instancia EC2 (nenhuma chave fica no codigo). Sem bucket (computador local), grava numa pasta do projeto.
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using SkiaSharp;

namespace MaxiMassas.Negocio;

public interface IFotoProdutoService
{
    /// <summary>Tamanho maximo do arquivo enviado (antes de reduzir).</summary>
    long TamanhoMaximoBytes { get; }

    /// <summary>Valida e converte para JPEG de ate 800 px. Lanca InvalidOperationException se nao for imagem valida.</summary>
    Task<byte[]> PrepararAsync(Stream arquivo, long tamanho);

    Task SalvarAsync(int produtoId, byte[] jpeg);

    /// <summary>Devolve o JPEG do produto, ou null se nao existir.</summary>
    Task<byte[]?> ObterAsync(int produtoId);

    Task ExcluirAsync(int produtoId);
}

public class FotoProdutoService : IFotoProdutoService, IDisposable
{
    public long TamanhoMaximoBytes => 5 * 1024 * 1024; // 5 MB

    private const int LadoMaximo = 800;
    private const int QualidadeJpeg = 85;

    private readonly string? _bucket;
    private readonly IAmazonS3? _s3;
    private readonly string _pastaLocal;

    public FotoProdutoService(IConfiguration config, IWebHostEnvironment ambiente)
    {
        var secao = config.GetSection("Fotos");
        _bucket = secao["Bucket"];
        _pastaLocal = Path.Combine(ambiente.ContentRootPath, secao["PastaLocal"] ?? "FotosProdutos");
        if (!string.IsNullOrWhiteSpace(_bucket))
            _s3 = new AmazonS3Client(RegionEndpoint.GetBySystemName(secao["Regiao"] ?? "us-east-1"));
    }

    private static string Chave(int produtoId) => $"produtos/{produtoId}.jpg";

    private string Caminho(int produtoId) => Path.Combine(_pastaLocal, $"{produtoId}.jpg");

    public async Task<byte[]> PrepararAsync(Stream arquivo, long tamanho)
    {
        if (tamanho <= 0) throw new InvalidOperationException("Escolha uma imagem.");
        if (tamanho > TamanhoMaximoBytes) throw new InvalidOperationException("A foto pode ter no maximo 5 MB.");

        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria);
        memoria.Position = 0;

        // Decodifica de fato (JPEG, PNG, WebP, GIF, BMP): o tipo informado pelo navegador nao e confiavel.
        using var original = SKBitmap.Decode(memoria)
            ?? throw new InvalidOperationException("O arquivo nao e uma imagem valida. Use JPG, PNG ou WebP.");

        // Mantem a proporcao; so reduz (nunca amplia).
        var escala = Math.Min(1f, (float)LadoMaximo / Math.Max(original.Width, original.Height));
        var largura = Math.Max(1, (int)Math.Round(original.Width * escala));
        var altura = Math.Max(1, (int)Math.Round(original.Height * escala));

        // JPEG nao tem transparencia: fundo branco atras de PNGs transparentes.
        using var superficie = SKSurface.Create(new SKImageInfo(largura, altura));
        superficie.Canvas.Clear(SKColors.White);
        using var pincel = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        superficie.Canvas.DrawBitmap(original, new SKRect(0, 0, largura, altura), pincel);
        using var imagem = superficie.Snapshot();
        using var jpeg = imagem.Encode(SKEncodedImageFormat.Jpeg, QualidadeJpeg);
        return jpeg.ToArray();
    }

    public async Task SalvarAsync(int produtoId, byte[] jpeg)
    {
        if (_s3 is not null)
        {
            using var fluxo = new MemoryStream(jpeg);
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = Chave(produtoId),
                InputStream = fluxo,
                ContentType = "image/jpeg"
            });
            return;
        }

        Directory.CreateDirectory(_pastaLocal);
        await File.WriteAllBytesAsync(Caminho(produtoId), jpeg);
    }

    public async Task<byte[]?> ObterAsync(int produtoId)
    {
        if (_s3 is not null)
        {
            try
            {
                using var resposta = await _s3.GetObjectAsync(_bucket, Chave(produtoId));
                using var memoria = new MemoryStream();
                await resposta.ResponseStream.CopyToAsync(memoria);
                return memoria.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        var caminho = Caminho(produtoId);
        return File.Exists(caminho) ? await File.ReadAllBytesAsync(caminho) : null;
    }

    public async Task ExcluirAsync(int produtoId)
    {
        if (_s3 is not null)
        {
            await _s3.DeleteObjectAsync(_bucket, Chave(produtoId));
            return;
        }

        var caminho = Caminho(produtoId);
        if (File.Exists(caminho)) File.Delete(caminho);
    }

    public void Dispose() => _s3?.Dispose();
}
