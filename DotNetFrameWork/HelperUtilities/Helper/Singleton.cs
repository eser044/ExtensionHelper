using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelperUtilities.Helper
{
    public class Singleton<T> where T : class
    {
        static object LockObject = new object();
        static T instance;
        
        private Singleton() { }
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (LockObject)
                    {
                        if (instance == null)
                        {
                            try
                            {
                                ConstructorInfo ci = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                                if (ci == null)
                                    throw new InvalidOperationException("class must contain a private constructor");

                                instance = (T)ci.Invoke(null);
                            }
                            catch (TargetInvocationException ex)
                            {
                                throw ex.InnerException;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                }
                return instance;
            }
        }
    }
}