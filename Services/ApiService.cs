using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataEntryApp.Models;

namespace DataEntryApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://ct-vic-drls.onrender.com/api";

    public ApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Entry>> GetEntriesAsync()
    {
        try 
        {
            return await _httpClient.GetFromJsonAsync<List<Entry>>($"{BaseUrl}/entries") ?? new List<Entry>();
        }
        catch (Exception)
        {
            return new List<Entry>();
        }
    }

    public async Task<bool> CreateEntryAsync(Entry entry)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/entries", entry);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateEntryAsync(Entry entry)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/entries/{entry.Id}", entry);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteEntryAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/entries/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
