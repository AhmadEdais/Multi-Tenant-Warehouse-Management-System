namespace WMS.Application.Common.Interfaces;

public interface ITreeNode<T>
{
    int Id { get; }
    int? ParentId { get; }
    List<T> Children { get; set; }
}