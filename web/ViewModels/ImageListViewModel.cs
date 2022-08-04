namespace BricksAndHearts.ViewModels;

public class ImageListViewModel
{
    public int PropertyId { get; set; }
    public List<ImageFileUrlModel>? FileList { get; set; }
}

public class ImageFileUrlModel
{
    public string FileName { get; set; }
    public string FileUrl { get; set; }

    public ImageFileUrlModel(string fileName, string fileUrl)
    {
        FileName = fileName;
        FileUrl = fileUrl;
    }
}