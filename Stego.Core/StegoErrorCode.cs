namespace Stego.Core;

public enum StegoErrorCode
{
    None = 0,
    FileNotFound = 1,
    UnsupportedFormat = 2,
    MessageTooLarge = 3,
    NoExifData = 4,
    EmptyExif = 5,
    SaveFailed = 6,
    InvalidOutputPath = 7,
    UnknownError = 255
}