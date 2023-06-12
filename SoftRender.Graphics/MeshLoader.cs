using SoftRender.SRMath;
using System.Globalization;

namespace SoftRender.Graphics
{
    public class MeshLoader
    {
        /// <summary>
        /// Loads an obj file and returns a vertex array with faces oriented counter clockwise.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Model Load(string filename)
        {
            // chat gpt

            // Temporary lists to hold the data as it's read
            var tempVertices = new List<Vector3D>();
            var tempUVs = new List<Vector2D>();
            var tempNormals = new List<Vector3D>();

            // Final lists to hold indexed data
            var vertices = new List<Vector3D>();
            var uvs = new List<Vector2D>();
            var normals = new List<Vector3D>();

            // Read the file line by line
            foreach (var line in File.ReadLines(filename))
            {
                // Split the line into components
                var parts = line.Split(' ');

                // Check if the line defines a vertex
                if (parts[0] == "v" && parts.Length == 4)
                {
                    // Parse the x, y, z coordinates
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture.NumberFormat);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture.NumberFormat);

                    // Add the vertex to the list
                    tempVertices.Add(new Vector3D(x, y, z));
                }
                // Check if the line defines a texture coordinate
                else if (parts[0] == "vt" && parts.Length >= 3)
                {
                    // Parse the u, v coordinates
                    float u = float.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat);
                    float v = float.Parse(parts[2], CultureInfo.InvariantCulture.NumberFormat);

                    // Add the uv to the list
                    tempUVs.Add(new Vector2D(u, v));
                }
                // Check if the line defines a vertex normal
                else if (parts[0] == "vn" && parts.Length == 4)
                {
                    // Parse the x, y, z coordinates
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture.NumberFormat);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture.NumberFormat);

                    // Add the normal to the list
                    tempNormals.Add(new Vector3D(x, y, z));
                }
                // Check if the line defines a face
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    // Parse each vertex/uv/normal index group
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var indices = parts[i].Split('/');

                        if (i == 4)
                        {
                            // Add vertex 1 and 3 to triangulate quads
                            // TODO: This code is bad
                            vertices.Add(tempVertices[int.Parse(parts[1].Split('/')[0]) - 1]);
                            vertices.Add(tempVertices[int.Parse(parts[3].Split('/')[0]) - 1]);
                            if (parts[1].Split('/').Length > 1 && parts[1].Split('/')[1] != "")
                            {
                                uvs.Add(tempUVs[int.Parse(parts[1].Split('/')[1]) - 1]);
                            }
                            if (parts[3].Split('/').Length > 1 && parts[3].Split('/')[1] != "")
                            {
                                uvs.Add(tempUVs[int.Parse(parts[3].Split('/')[1]) - 1]);
                            }
                            if (parts[1].Split('/').Length > 2 && parts[1].Split('/')[2] != "")
                            {
                                normals.Add(tempNormals[int.Parse(parts[1].Split('/')[2]) - 1]);
                            }
                            if (parts[3].Split('/').Length > 2 && parts[3].Split('/')[2] != "")
                            {
                                normals.Add(tempNormals[int.Parse(parts[3].Split('/')[2]) - 1]);
                            }
                        }

                        // Add the indexed attributes to the final lists
                        vertices.Add(tempVertices[int.Parse(indices[0]) - 1]);

                        if (indices.Length > 1 && indices[1] != "")
                        {
                            uvs.Add(tempUVs[int.Parse(indices[1]) - 1]);
                        };
                        if (indices.Length > 2 && indices[2] != "")
                        {
                            normals.Add(tempNormals[int.Parse(indices[2]) - 1]);
                        };
                    }
                }
            }

            var attribs = new VertexAttributes[vertices.Count];
            for (int i = 0; i < attribs.Count(); i++)
            {
                attribs[i] = new VertexAttributes(uvs[i].X, uvs[i].Y, normals[i].X, normals[i].Y, normals[i].Z);
            }

            return new Model()
            {
                Vertices = vertices.ToArray(),
                Attributes = attribs.ToArray()
            };
        }
    }
}
