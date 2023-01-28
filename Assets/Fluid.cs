using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluid : MonoBehaviour
{
    public Vector2 gridSize = new Vector2(100, 100);  // number of cells in x and y
    public Vector2 force = new Vector2(0, -9.8f);  // external force (e.g. gravity)
    public double dt = 0.01;
    public double rho = 1000;  // fluid density
    public double h = 0.01;    // simulation scale (meters/cell)
    public double rel = 1.9;  // relaxation factor

    double[,] p;
    double[,] u;
    double[,] v;
    double[,] phi;
    double[,] div;
    int[,] s;
    int cellsX;
    int cellsY;


    void InitializeFields() {
        cellsX += 2;
        cellsY += 2;
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                p[x,y] = 0;
                u[x,y] = 0;
                v[x,y] = 0;
                phi[x,y] = 0;
                div[x,y] = 0;
                if (x == 0 || x == cellsX - 1 || y == 0 || y == cellsY - 1) {
                    s[x,y] = 1;
                }
                else {
                    s[x,y] = 0;
                }
            }
        }
        Debug.Log("Fields initialized.");
    }


    void SimulateTimestep() {
        ApplyForces();
        ApplyProjection();
        ApplyAdvection();
    }


    void ApplyForces() {
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                if (s[x,y] == 0) {
                    u[x,y] += force[0] * dt;
                    v[x,y] += force[1] * dt;
                }
            }
        }
    }


    void ApplyProjection(int nIter = 50) {
        for (int x = 0; x < cellsX; x++) {
            for (int y = 0; y < cellsY; y++) {
                p[x,y] = 0;
            }
        }
        for (int n = 0; n < nIter; n++) {
            for (int x = 0; x < cellsX; x++) {
                for (int y = 0; y < cellsY; y++) {
                    if (s[x,y] == 0) {
                        double divergence = rel * (-u[x, y] + u[x+1, y] - v[x, y] + v[x, y+1]);
                        int s_total = s[x-1, y] + s[x, y-1] + s[x+1, y] + s[x, y+1];

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
        null;
    }



    void Start() {
        InitializeFields();
    }

    void Update()
    {
        
    }
}
