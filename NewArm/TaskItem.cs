using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewArm
{
    public enum Command
    {
        mouseMovStatic = 0, mouseMov = 1, mouseLC = 2, mouseLD = 3, mouseLU = 4, mouseRC = 5, mouseRD = 6, mouseRU = 7, key = 8, wait = 9
    };
    public class TaskItem
    {
        public Command command;
        public string args = string.Empty;
    }
}
