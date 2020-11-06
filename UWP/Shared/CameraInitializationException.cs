using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CameraInitializationException : Exception
{
    public CameraInitializationException()
    {

    }

    public CameraInitializationException(string message) : base(message)
    {
        Debug.WriteLine(message);
    }

    public CameraInitializationException(string message, Exception inner) : base(message, inner)
    {
        Debug.WriteLine(message);
        Debug.WriteLine(inner);
    }
}
