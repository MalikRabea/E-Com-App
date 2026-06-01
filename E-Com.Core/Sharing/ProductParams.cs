using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Sharing
{
    public class ProductParams
    {
        public string? Sort       { get; set; }
        public int?    CategoryId { get; set; }
        public string? Search     { get; set; }

        // Price filter
        public decimal? MinPrice  { get; set; }
        public decimal? MaxPrice  { get; set; }

        public int MaxPageSize { get; set; } = 50;

        private int _pageSize = 9;
        public int pageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
        public int PageNumber { get; set; } = 1;
    }
}
