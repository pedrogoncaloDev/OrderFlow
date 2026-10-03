using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using OrderFlow.Web.Models;

namespace OrderFlow.Web.Services;

/// <summary>
/// Diz ao Blazor quem é o usuário atual. O token JWT fica no localStorage do navegador, mas
/// criptografado pelo servidor (ProtectedLocalStorage): o JavaScript da página não consegue lê-lo.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string StorageKey = "orderflow.session";

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly ProtectedLocalStorage _storage;

    private bool _loaded;
    private string? _accessToken;
    private DateTime _expiresAtUtc;
    private ClaimsPrincipal _principal = Anonymous;

    // Construtor
    public JwtAuthenticationStateProvider(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    /// <summary>Quando a sessão atual expira (nulo se não houver sessão).</summary>
    public DateTime? SessionExpiresAtUtc => _accessToken is null ? null : _expiresAtUtc;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();
        DiscardIfExpired();

        return new AuthenticationState(_principal);
    }

    /// <summary>Token para chamar a API, ou nulo se não houver sessão válida.</summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        await EnsureLoadedAsync();
        DiscardIfExpired();

        return _accessToken;
    }

    public async Task SignInAsync(AuthResponse response)
    {
        var principal = BuildPrincipal(response.AccessToken)
            ?? throw new InvalidOperationException("A API devolveu um token de acesso inválido.");

        await _storage.SetAsync(StorageKey, new StoredSession(response.AccessToken, response.ExpiresAtUtc));

        Apply(response.AccessToken, response.ExpiresAtUtc, principal);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_principal)));
    }

    public async Task SignOutAsync()
    {
        Clear();

        try
        {
            await _storage.DeleteAsync(StorageKey);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException)
        {
            // Sem JavaScript disponível (circuito encerrado): a sessão já foi limpa da memória.
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_principal)));
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded)
            return;

        try
        {
            var result = await _storage.GetAsync<StoredSession>(StorageKey);

            if (result.Success && result.Value is { } session && session.ExpiresAtUtc > DateTime.UtcNow)
            {
                var principal = BuildPrincipal(session.AccessToken);
                if (principal is not null)
                    Apply(session.AccessToken, session.ExpiresAtUtc, principal);
            }

            _loaded = true;
        }
        catch (CryptographicException)
        {
            // Dado ilegível (por exemplo, as chaves de proteção mudaram): segue como anônimo.
            _loaded = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSDisconnectedException)
        {
            // Sem JavaScript ainda (renderização estática): segue como anônimo e tenta de novo depois.
        }
    }

    private void DiscardIfExpired()
    {
        if (_accessToken is not null && _expiresAtUtc <= DateTime.UtcNow)
            Clear();
    }

    private void Apply(string accessToken, DateTime expiresAtUtc, ClaimsPrincipal principal)
    {
        _accessToken = accessToken;
        _expiresAtUtc = expiresAtUtc;
        _principal = principal;
        _loaded = true;
    }

    private void Clear()
    {
        _accessToken = null;
        _expiresAtUtc = default;
        _principal = Anonymous;
        _loaded = true;
    }

    private static ClaimsPrincipal? BuildPrincipal(string accessToken)
    {
        try
        {
            var claims = JwtClaimsParser.Parse(accessToken);
            var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "email", roleType: "role");

            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
