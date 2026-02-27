using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mangareading.DTOs
{
    // Cấu trúc chung cho các response từ MangaDex API
    public class MangaDexResponse<T>
    {
        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }
    }

    // DTO cho manga
    public class MangaDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public MangaAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; }
    }

    public class MangaAttributes
    {
        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; }

        [JsonPropertyName("altTitles")]
        public List<Dictionary<string, string>> AltTitles { get; set; }

        [JsonPropertyName("description")]
        public Dictionary<string, string> Description { get; set; }

        [JsonPropertyName("originalLanguage")]
        public string OriginalLanguage { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("tags")]
        public List<TagDto> Tags { get; set; }

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; }
    }

    public class TagDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public TagAttributes Attributes { get; set; }
    }

    public class TagAttributes
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; }
    }

    // DTO cho chapter
    public class ChapterDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("attributes")]
        public ChapterAttributes Attributes { get; set; }

        [JsonPropertyName("relationships")]
        public List<Relationship> Relationships { get; set; }
    }

    public class ChapterAttributes
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("volume")]
        public string Volume { get; set; }

        [JsonPropertyName("chapter")]
        public string Chapter { get; set; }

        [JsonPropertyName("translatedLanguage")]
        public string TranslatedLanguage { get; set; }

        [JsonPropertyName("publishAt")]
        public DateTime PublishAt { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }
    }

    // DTO cho việc đọc chapter
    public class ChapterReadDto
    {
        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonPropertyName("chapter")]
        public ChapterDataDto Chapter { get; set; }
    }

    public class ChapterDataDto
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("data")]
        public List<string> Data { get; set; }

        [JsonPropertyName("dataSaver")]
        public List<string> DataSaver { get; set; }
    }

    // DTO chung cho relationship
    public class Relationship
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public object Attributes { get; set; }
    }

    // DTO cho cover art
    public class CoverAttributes
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
    }

    // DTO cho author
    public class AuthorAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}