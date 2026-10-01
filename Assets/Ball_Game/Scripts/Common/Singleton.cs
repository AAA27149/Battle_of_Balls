
public class Singleton<T> where T : new() //泛型单例
{
    private static T instance;
    private static object locker = new object();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }

            return instance;
        }
    }
}
