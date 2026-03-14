using System.Windows;
using System.Windows.Controls;
using Witcher3StringEditor.Dialogs.Models;

namespace Witcher3StringEditor.Dialogs.TemplateSelectors
{
    /// <summary>
    ///    A DataTemplateSelector that selects the appropriate data template based on the type of dictionary tree view model
    /// </summary>
    public class DictionaryTreeTemplateSelector:DataTemplateSelector
    {
        /// <summary>
        ///    Gets or sets the data template for dictionary group
        /// </summary>
        public DataTemplate? DictionaryGroupTemplate { get; set; }
        
        /// <summary>
        ///    Gets or sets the data template for dictionary item
        /// </summary>
        public DataTemplate? DictionaryItemTemplate { get; set; }
        
        /// <summary>
        ///    Selects the appropriate data template based on the type of the item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns>
        ///    The DataTemplate to use for the item, or null if no appropriate template is found
        /// </returns>
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            return item switch
            {
                DictionaryGroup => DictionaryGroupTemplate,
                string => DictionaryItemTemplate,
                _ => null
            };        
        }
    }
}
