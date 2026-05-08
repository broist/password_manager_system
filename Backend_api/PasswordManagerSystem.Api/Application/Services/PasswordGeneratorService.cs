using System.Security.Cryptography;
using PasswordManagerSystem.Api.Application.DTOs.PasswordGenerator;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Application.Services;

public class PasswordGeneratorService : IPasswordGeneratorService
{
    private const string UppercaseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowercaseCharacters = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitCharacters = "0123456789";
    private const string SpecialCharacters = "!@#$%^&*()-_=+[]{};:,.<>?";

    public GeneratePasswordResponse Generate(GeneratePasswordRequest request)
    {
        var characterSets = new List<string>();

        if (request.IncludeUppercase)
        {
            characterSets.Add(UppercaseCharacters);
        }

        if (request.IncludeLowercase)
        {
            characterSets.Add(LowercaseCharacters);
        }

        if (request.IncludeDigits)
        {
            characterSets.Add(DigitCharacters);
        }

        if (request.IncludeSpecialCharacters)
        {
            characterSets.Add(SpecialCharacters);
        }

        var allCharacters = string.Concat(characterSets);

        var passwordCharacters = new List<char>();

        foreach (var characterSet in characterSets)
        {
            passwordCharacters.Add(GetRandomCharacter(characterSet));
        }

        while (passwordCharacters.Count < request.Length)
        {
            passwordCharacters.Add(GetRandomCharacter(allCharacters));
        }

        Shuffle(passwordCharacters);

        var password = new string(passwordCharacters.ToArray());

        return new GeneratePasswordResponse
        {
            Password = password,
            Length = password.Length,
            IncludesUppercase = request.IncludeUppercase,
            IncludesLowercase = request.IncludeLowercase,
            IncludesDigits = request.IncludeDigits,
            IncludesSpecialCharacters = request.IncludeSpecialCharacters
        };
    }

    private static char GetRandomCharacter(string characters)
    {
        var index = RandomNumberGenerator.GetInt32(characters.Length);

        return characters[index];
    }

    private static void Shuffle(IList<char> characters)
    {
        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);

            (characters[i], characters[j]) = (characters[j], characters[i]);
        }
    }
}