using MediaIngest.Foundation;

if (FoundationMetadata.SolutionName != "MediaIngest")
{
    throw new InvalidOperationException("The foundation project metadata does not identify the MediaIngest solution.");
}

Console.WriteLine("MediaIngest foundation smoke test passed.");
