using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum DISPLAY_FIELD {
    PRESSURE = 0,
    U_VELOCITY = 1,
    V_VELOCITY = 2,
    PHI = 3,
    DIVERGENCE = 4,
    S = 5
}

public class Fluid : MonoBehaviour
{
    public DISPLAY_FIELD displayField;
    public Vector2 gridSize = new Vector2(100, 100);  // number of cells in x and y
    public Vector2 force = new Vector2(0, -9.8f);  // external force (e.g. gravity)
    public double dt = 0.01;
    public double rho = 1000;  // fluid density        double max = field.Cast<double>().Max();
    public double h = 0.01;    // simulation scale (meters/cell)
    public double rel = 1.9;  // relaxation factor
    public int pIter = 50;    // pressure projection iterations

    [HideInInspector] public int cellsX;
    [HideInInspector] public int cellsY;

    [HideInInspector] public double[,] p;
    [HideInInspector] public double[,] u;
    [HideInInspector] public double[,] v;
    [HideInInspector] public double[,] phi;
    [HideInInspector] public double[,] div;
    [HideInInspector] public double[,] s;  // only needs to be int, but using double for hacky workaround (see e.g. UpdateTexture())

    Texture2D texture;


    void InitializeFields() {
        cellsX = (int)gridSize[0] + 2;
        cellsY = (int)gridSize[1] + 2;

        p = new double[cellsX, cellsY];
        u = new double[cellsX, cellsY];
        v = new double[cellsX, cellsY];
        phi = new double[cellsX, cellsY];
        div = new double[cellsX, cellsY];
        s = new double[cellsX, cellsY];

        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                p[x,y] = 0;
                u[x,y] = 0;
                v[x,y] = 0;
                phi[x,y] = 0;
                div[x,y] = 0;
                if (x == 0 || x == cellsX - 1 || y == 0 || y == cellsY - 1) {
                    s[x,y] = 0;
                }
                else {
                    s[x,y] = 1;
                }
            }
        }

        SetTestState();

        Debug.Log("Fields initialized.");
    }


    void SetTestState() {
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                if (x > 10 && x < 90 && y > 35 && y < 65) {
                    phi[x,y] = 1;
                }
                if (x > 50 && x < 67 && y > 30 && y < 40) {
                    s[x,y] = 0;
                }
            }
        }
    }


    void SimulateTimestep() {
        AddSmoke();
        ApplyForces();
        ApplyProjection(pIter);
        ApplyAdvection();
    }


    void ApplyForces() {
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                if (s[x,y] == 1) {
                    u[x,y] += force[0] * dt;
                    v[x,y] += force[1] * dt;
                    if (x > 35 && x < 42 && y > 15 && y < 45) {
                        u[x,y] += 180 * dt;
                    }
                }
            }
        }
        //Debug.Log(v[32,66]);
    }


    void ApplyProjection(int nIter = 2) {
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                p[x,y] = 0;
            }
        }
        for (int n = 0; n < nIter; n++) {
            //Debug.Log(v[32,66]);
            for (int x = 0; x < cellsX; x++) {
                for (int y = 0; y < cellsY; y++) {
                    //Debug.Log(s[x,y]);
                    if (s[x,y] == 1) {
                        double divergence = rel * (-u[x, y] + u[x+1, y] - v[x, y] + v[x, y+1]);
                        int s_total = (int)(s[x-1, y] + s[x, y-1] + s[x+1, y] + s[x, y+1]);

                        //Debug.Log("Divergence: " + divergence);
                        //Debug.Log("s_total: " + s_total);

                        div[x,y] = divergence / rel;

                        u[x,y] += s[x-1, y] * divergence / s_total;
                        u[x+1, y] -= s[x+1, y] * divergence / s_total;
                        v[x,y] += s[x, y-1] * divergence / s_total;
                        v[x, y+1] -= s[x, y+1] * divergence / s_total;

                        p[x,y] += (divergence / s_total) * rho * h / dt;
                    }
                }
            }
        }
    }


    void ApplyAdvection() {
        double[,] u_new = new double[cellsX, cellsY];
        double[,] v_new = new double[cellsX, cellsY];
        double[,] phi_new = new double[cellsX, cellsY];

        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                if (s[x,y] == 1) {
                    double[] new_uv = StaggeredGrid.AdvectVelocity(this, x, y);
                    u_new[x,y] = new_uv[0];
                    v_new[x,y] = new_uv[1];
                    phi_new[x,y] = StaggeredGrid.AdvectScalar(this, phi, x, y);
                }
            }
        }

        u = u_new;
        v = v_new;
        phi = phi_new;
    }

    
    void SetTexture() {
        texture = new Texture2D(cellsX, cellsY);
        GetComponent<RawImage>().texture = texture;
    }


    void UpdateTexture(double[,] field) {
        double max;
        if (field == phi) {
            max = 1;
        }
        else {
            max = field.Cast<double>().Max();
        }
        //Debug.Log(max);
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                float val = (float)(field[x,y] / max);
                Color pixelColor = new Color(val, val, val, 1);
                texture.SetPixel(x, y, pixelColor);
            }
        }
        texture.Apply();
    }


    void DrawField() {
        double[,] field = p;

        switch(displayField) {
            case DISPLAY_FIELD.PRESSURE:
                field = p;
                break;
            case DISPLAY_FIELD.U_VELOCITY:
                field = u;
                break;        
            case DISPLAY_FIELD.V_VELOCITY:
                field = v;
                break;        
            case DISPLAY_FIELD.PHI:
                field = phi;
                break;
            case DISPLAY_FIELD.DIVERGENCE:
                field = div;
                break;
            case DISPLAY_FIELD.S:
                field = s;
                break;        
        }

        UpdateTexture(field);
    }


    void AddSmoke() {
        RawImage image = GetComponent<RawImage>();

        Vector3[] corners = new Vector3[4];

        image.rectTransform.GetWorldCorners(corners);
        Rect rect = new Rect(corners[0], corners[2] - corners[0]);

        int x = (int)((Input.mousePosition.x - rect.x) * (cellsX / rect.size[0]));
        int y = (int)((Input.mousePosition.y - rect.y) * (cellsY / rect.size[1]));

        if (x > 0 && x < cellsX && y > 0 && y < cellsY) {
            if (s[x,y] == 1) {
                phi[x,y] = 1;
            }
        }
    }


    void Start() {
        InitializeFields();
        SetTexture();
    }

    void Update()
    {
        SimulateTimestep();
        DrawField();
    }
}
