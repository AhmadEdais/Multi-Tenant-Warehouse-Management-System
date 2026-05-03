namespace WMS.Application.Common.Extensions;

public static class TreeExtensions
{
    public static List<T> BuildTree<T>(this IEnumerable<T> flatList) where T : class, ITreeNode<T>
    {
        var lookup = flatList.ToDictionary(x => x.Id);
        var rootNodes = new List<T>();

        foreach (var item in flatList)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                rootNodes.Add(item);
            }
        }

        return rootNodes;
    }
}