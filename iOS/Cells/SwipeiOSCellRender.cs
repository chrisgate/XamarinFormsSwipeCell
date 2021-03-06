﻿﻿using System;
using CustomCell;
using CustomCell.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Xamarin.Forms.Internals;

[assembly: ExportRenderer(typeof(SwipeCell), typeof(SwipeiOSCellRender))]
namespace CustomCell.iOS
{
    public class SwipeiOSCellRender : ViewCellRenderer
    {
        SwipeiOSCell cell;

        public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
        {
            var swipeCell = (SwipeCell)item;
            cell = reusableCell as SwipeiOSCell;

            if (cell == null)
            {
                cell = new SwipeiOSCell(item.GetType().FullName, swipeCell);
            }

            var nativeCell = base.GetCell(item, reusableCell, tv);

            cell.Update(tv, swipeCell, nativeCell);
            
            nativeCell = cell;

            return nativeCell;
        }
    }
}
;