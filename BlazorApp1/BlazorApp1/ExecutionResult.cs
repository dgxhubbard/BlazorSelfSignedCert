using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BlazorApp1.Enums;
using Microsoft.AspNetCore.Http;

namespace BlazorApp1
{
    public class ExecutionResult
    {
        public StatusCode Status
        { get; set; }

        public string ErrorMessage
        { get; set; }
    }
}
