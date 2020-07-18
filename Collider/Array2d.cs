namespace Collider
{
    public class Array2D
    {
        public static T[][] Create<T>(int dim1, int dim2)
        {
            var array2D = new T[dim1][];
            for (int i = 0; i < dim1; i++)
            {
                array2D[i] = new T[dim2];
            }

            return array2D;
        }

    }
}
