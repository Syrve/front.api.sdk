using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Organization.Sections;

namespace Resto.Front.Api.SamplePlugin.Restaurant
{
    public sealed partial class MenuView
    {
        public MenuView()
        {
            InitializeComponent();

            ReloadRestaurant();
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            ReloadRestaurant();
        }

        private void ReloadRestaurant()
        {
            var menu = PluginContext.Operations.GetHierarchicalMenu();
            treeMenu.ItemsSource = ConcatProductsAndGroups(menu.Products, menu.ProductGroups).ToList();
        }

        [NotNull]
        private IEnumerable<object> ConcatProductsAndGroups([NotNull] IEnumerable<IProduct> products, [NotNull] IEnumerable<IProductGroup> productGroups)
        {
            foreach (var productGroupModel in productGroups.Select(productGroup => new ProductGroupModel(productGroup)))
                yield return productGroupModel;

            var remainingAmounts = PluginContext.Operations.GetProductsRemainingAmounts();
            foreach (var product in products)
            {
                var remainingAmount = remainingAmounts.ContainsKey(product) ? remainingAmounts[product] : (decimal?)null;
                var isSellingRestricted = PluginContext.Operations.IsProductSellingRestricted(product);
                var includedInMenuSections = PluginContext.Operations.GetIncludedInMenuSectionsByProduct(product);
                yield return new ProductModel(product, includedInMenuSections, remainingAmount, isSellingRestricted);
            }
        }

        private void OnProductGroupExpanded(object sender, RoutedEventArgs e)
        {
            var productGroupModel = (ProductGroupModel)((TreeViewItem)sender).DataContext;
            if (!productGroupModel.HasFakeItem)
                return;

            var childProducts = PluginContext.Operations.GetChildProductsByProductGroup(productGroupModel.ProductGroup);
            var childGroups = PluginContext.Operations.GetChildGroupsByProductGroup(productGroupModel.ProductGroup);
            productGroupModel.ReplaceItems(ConcatProductsAndGroups(childProducts, childGroups));
        }

		 /// To correctly display the list of modifiers, you need to specify the correct price category.
         /// If the establishment does not use price categories, you can pass <c>null</c>.
         /// You can also get the price category from
         /// <see cref="IRestaurantSection.DefaultPriceCategory" /> - Price category assigned to the branch;
         /// <see cref="Data.Organization.ITerminalsGroup.PriceCategory" /> - Price category of the branch that owns the table by default;
         /// <see cref="Data.Orders.IOrder.PriceCategory" /> - Price category assigned to the order;
         /// <see cref="IOperationService.GetPriceCategories" /> - list of price categories;
         /// <see cref="IOperationService.GetPriceCategoryById" /> - get price category by id.
        private void OnProductExpanded(object sender, RoutedEventArgs e)
        {
            var productModel = (ProductModel)((TreeViewItem)sender).DataContext;
            if (!productModel.HasFakeItem)
                return;

            // The price category of the branch in which the default table is located
            var priceCategory = PluginContext.Operations.GetHostTerminalsGroup().PriceCategory;

            var groupModifiers = productModel.Product.GetGroupModifiers(priceCategory);
            var simpleModifiers = productModel.Product.GetSimpleModifiers(priceCategory);
            productModel.ReplaceItems(groupModifiers.Cast<object>().Concat(simpleModifiers));
        }
    }
}
