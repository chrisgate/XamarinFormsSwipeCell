using System;
using CustomCell.Cells;
using CustomCell.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using UIKit;

[assembly: ExportRenderer(typeof(SwipeCell), typeof(SwipeiOSCellRender))]
namespace CustomCell.iOS
{
    public class SwipeiOSCellRender : ViewCellRenderer
    {
        SwipeiOSCell cell;

        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var result = base.GetCell(item, reusableCell, tv);
            //WireUpForceUpdateSizeRequested(item, result, tv);

            //return result;
            var swipeCell = (SwipeCell)item;
            cell = reusableCell as SwipeiOSCell;

            if(cell == null)
            {
                cell = new SwipeiOSCell(item.GetType().FullName, swipeCell);
            }

            cell.Update(tv, swipeCell, result);

            return cell;
        }
    }
}
