using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Com.Core.Sharing
{
    public class ProductParams
    {
        //string sort , int? CategoryId , int pageSize , int pageNumber
        public string? Sort { get; set; }
        public int? CategoryId { get; set; }

        public string? Search { get; set; }
        public int MaxPageSize { get; set; } = 6; // Default page size

        private int _pageSize = 3; // Default page number

        public int pageSize
        {
            get  { return _pageSize; }
            set { _pageSize = value > MaxPageSize ? MaxPageSize : value; }
        }
        public int PageNumber { get; set; } = 1; // Default page number

    }
}
