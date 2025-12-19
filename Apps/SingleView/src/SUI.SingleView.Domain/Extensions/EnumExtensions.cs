namespace SUI.SingleView.Domain.Extensions;

public static class EnumExtensions
{
    public static string ToFriendlyEnumString<T>(this T enumValue)
        where T : struct
    {
        var str = $"{enumValue}";
        return string.Concat(
            str.Select(
                (c, i) =>
                {
                    var prev = str.ElementAtOrDefault(i - 1);
                    var next = str.ElementAtOrDefault(i + 1);
                    var prepend =
                        (char.IsUpper(prev) && char.IsUpper(c) && char.IsLower(next)) // Acronym case: AAFoo -> AA Foo
                        || (char.IsNumber(prev) && char.IsUpper(c)); // Split number and word: 123Foo -> 123 Foo
                    var append = char.IsLower(c) && (char.IsUpper(next) || char.IsNumber(next)); // Split end of word: FooBar -> Foo Bar, Foo123 -> Foo 123
                    return $"{(prepend ? " " : "")}{c}{(append ? " " : "")}";
                }
            )
        );
    }
}
