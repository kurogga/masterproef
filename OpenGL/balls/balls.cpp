#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <vector>
#include <algorithm>

#include <GL/glew.h>

#include <GLFW/glfw3.h>
GLFWwindow* window;

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtx/norm.hpp>
using namespace glm;

#include <common/shader.hpp>
#include <common/texture.hpp>
#include <common/controls.hpp>

const float size = 0.2f;
const int maxObjects = 10;

struct FallingObject {
	glm::vec3 pos, speed, color;
	
	void bounceObject() {
		//std::cout << "pos: " << pos[0] << "," << pos[1] << std::endl;
		float minus = -1.0f;
		if (this->pos[1] <= -1.0f) {// bottom border
			this->speed = glm::vec3(this->speed) * minus;
			this->color[0] = rand() % 1000 / 1000.0;
			this->color[1] = rand() % 1000 / 1000.0;
			this->color[2] = rand() % 1000 / 1000.0;
		}
		else if (this->pos[1] >= 1.0f) { // top border
			this->speed = glm::vec3(this->speed) * minus;
			this->color[0] = rand() % 1000 / 1000.0;
			this->color[1] = rand() % 1000 / 1000.0;
			this->color[2] = rand() % 1000 / 1000.0;
		}
	}
};
std::vector<FallingObject> allObjects;

void checkAllObjectsBorder() {
	for (int i = 0; i < allObjects.size(); i++) {
		allObjects[i].bounceObject();
	}
}

int main(void)
{
	// Initialise GLFW
	if (!glfwInit())
	{
		fprintf(stderr, "Failed to initialize GLFW\n");
		getchar();
		return -1;
	}

	glfwWindowHint(GLFW_SAMPLES, 4);
	glfwWindowHint(GLFW_RESIZABLE, GL_FALSE);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
	glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE); // To make MacOS happy; should not be needed
	glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

	// Open a window and create its OpenGL context
	window = glfwCreateWindow(1080, 1080, "Balls", NULL, NULL);
	if (window == NULL) {
		fprintf(stderr, "Failed to open GLFW window. If you have an Intel GPU, they are not 3.3 compatible. Try the 2.1 version of the tutorials.\n");
		getchar();
		glfwTerminate();
		return -1;
	}
	glfwMakeContextCurrent(window);

	// Initialize GLEW
	glewExperimental = true; // Needed for core profile
	if (glewInit() != GLEW_OK) {
		fprintf(stderr, "Failed to initialize GLEW\n");
		getchar();
		glfwTerminate();
		return -1;
	}

	// Ensure we can capture the escape key being pressed below
	glfwSetInputMode(window, GLFW_STICKY_KEYS, GL_TRUE);

	// Set background color
	glClearColor(0.5f, 0.5f, 0.5f, 0.0f);

	GLuint VertexArrayID;
	glGenVertexArrays(1, &VertexArrayID);
	glBindVertexArray(VertexArrayID);

	// Create and compile our GLSL program from the shaders
	GLuint programID = LoadShaders("ball.vertexshader", "ball.fragmentshader");

	// vertex data and buffer
	static GLfloat g_vertex_buffer_data[] = {
		-0.5f, -0.5f, 0.0f,
		  0.5f, -0.5f, 0.0f,
		 -0.5f,  0.5f, 0.0f,
		  0.5f,  0.5f, 0.0f
	};
	GLuint vertexbuffer;
	glGenBuffers(1, &vertexbuffer);
	glBindBuffer(GL_ARRAY_BUFFER, vertexbuffer);
	glBufferData(GL_ARRAY_BUFFER, sizeof(g_vertex_buffer_data), g_vertex_buffer_data, GL_STATIC_DRAW);

	FallingObject obj;
	obj.pos = glm::vec3(0.035f, 0.945f, 0.0f);
	obj.speed = glm::vec3(0.0f, -2.0f, 0.0f);
	obj.color = glm::vec3(0.583f, 0.771f, 0.014f);
	allObjects.push_back(obj);

	// color data and buffer
	static GLfloat* g_color_buffer_data = new GLfloat[maxObjects * 3];
	GLuint colorbuffer;
	glGenBuffers(1, &colorbuffer);
	glBindBuffer(GL_ARRAY_BUFFER, colorbuffer);
	glBufferData(GL_ARRAY_BUFFER, maxObjects * 3 * sizeof(GLfloat), NULL, GL_DYNAMIC_DRAW);

	// center position buffer
	static GLfloat* g_centerpos_buffer_data = new GLfloat[maxObjects * 3];
	GLuint centerposbuffer;
	glGenBuffers(1, &centerposbuffer);
	glBindBuffer(GL_ARRAY_BUFFER, centerposbuffer);
	glBufferData(GL_ARRAY_BUFFER, maxObjects*3*sizeof(GLfloat), NULL, GL_DYNAMIC_DRAW);

	//glEnable(GL_PROGRAM_POINT_SIZE);
	int objectCount = 1;
	int nbFrames = 0;
	double lastTime = glfwGetTime();
	double lastFrameTime = glfwGetTime();
	do
	{
		// Clear the screen
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		double currentTime = glfwGetTime();
		nbFrames++;
		if (currentTime - lastTime >= 1.0) { // If last prinf() was more than 1 sec ago
			// printf and reset timer
			printf("%.3f ms/frame\n", 1000.0 / double(nbFrames));
			printf("%d fps\n", nbFrames);
			nbFrames = 0;
			lastTime += 1.0;
		}
		if (objectCount < maxObjects) {
			FallingObject obj2;
			obj2.pos = glm::vec3((rand() % 2000 - 1000.0f) / 1000.0f,
				0.945f, 0.0f);
			obj2.speed = glm::vec3(0.0f, (rand()%1000/200.0f) - 6.0f, 0.0f);
			obj2.color = glm::vec3(rand() % 1000 / 1000.0, rand() % 1000 / 1000.0, rand() % 1000 / 1000.0);
			allObjects.push_back(obj2);
			objectCount++;
			//printf("Object %d created on position: %.3fx %.3fy\n", objectCount, obj2.pos[0], obj2.pos[1]);
		}
		double delta = currentTime - lastFrameTime;
		lastFrameTime = currentTime;

		checkAllObjectsBorder();

		// Simulate all objects
		for (int i = 0; i < allObjects.size(); i++) {
			float pos = allObjects[i].speed[1] * (float)delta;
			allObjects[i].pos[1] += pos;
			// Fill the GPU buffer
			g_centerpos_buffer_data[3 * i] = allObjects[i].pos[0];
			g_centerpos_buffer_data[3 * i + 1] = allObjects[i].pos[1];
			g_centerpos_buffer_data[3 * i + 2] = allObjects[i].pos[2];

			g_color_buffer_data[3 * i] = allObjects[i].color[0];
			g_color_buffer_data[3 * i + 1] = allObjects[i].color[1];
			g_color_buffer_data[3 * i + 2] = allObjects[i].color[2];
			//printf("Object %d passed to GPU buffer\n", i);
			//printf("position: %.3f\n", allObjects[i].pos[1]);
		}

		// Update the buffer data
		glBindBuffer(GL_ARRAY_BUFFER, colorbuffer);
		glBufferData(GL_ARRAY_BUFFER, maxObjects * 3 * sizeof(GLfloat), NULL, GL_DYNAMIC_DRAW); // Buffer orphaning, a common way to improve streaming perf. See above link for details.
		glBufferSubData(GL_ARRAY_BUFFER, 0, objectCount * sizeof(GLfloat) * 3, g_color_buffer_data);

		glBindBuffer(GL_ARRAY_BUFFER, centerposbuffer);
		glBufferData(GL_ARRAY_BUFFER, maxObjects * 3 * sizeof(GLfloat), NULL, GL_DYNAMIC_DRAW); 
		glBufferSubData(GL_ARRAY_BUFFER, 0, objectCount * sizeof(GLfloat) * 3, g_centerpos_buffer_data);

		glUseProgram(programID);

		// 1st attribute buffer : vertices
		glEnableVertexAttribArray(0);
		glBindBuffer(GL_ARRAY_BUFFER, vertexbuffer);
		glVertexAttribPointer(
			0,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
			3,                  // size
			GL_FLOAT,           // type
			GL_FALSE,           // normalized?
			0,                  // stride
			(void*)0            // array buffer offset
		);

		// 2nd attribute buffer : colors
		glEnableVertexAttribArray(1);
		glBindBuffer(GL_ARRAY_BUFFER, colorbuffer);
		glVertexAttribPointer(
			1,                                // attribute. No particular reason for 1, but must match the layout in the shader.
			3,                                // size
			GL_FLOAT,                         // type
			GL_FALSE,                         // normalized?
			0,                                // stride
			(void*)0                          // array buffer offset
		);

		// 3rd attribute buffer : center position
		glEnableVertexAttribArray(2);
		glBindBuffer(GL_ARRAY_BUFFER, centerposbuffer);
		glVertexAttribPointer(
			2,                                // attribute. No particular reason for 2, but must match the layout in the shader.
			3,                                // size
			GL_FLOAT,                         // type
			GL_FALSE,                         // normalized?
			0,                                // stride
			(void*)0                          // array buffer offset
		);

		// These functions are specific to glDrawArrays*Instanced*.
		// The first parameter is the attribute buffer we're talking about.
		// The second parameter is the "rate at which generic vertex attributes advance when rendering multiple instances"
		glVertexAttribDivisor(0, 0); // particles vertices : always reuse the same 4 vertices -> 0
		glVertexAttribDivisor(1, 1); // positions : one per quad (its center)                 -> 1
		glVertexAttribDivisor(2, 1); // color : one per quad                                  -> 1

		
		// Draw the traingle
		glDrawArraysInstanced(GL_TRIANGLE_STRIP, 0, 4, objectCount); // 3 indices starting at 0 -> 1 triangle

		glDisableVertexAttribArray(0);
		glDisableVertexAttribArray(1);
		glDisableVertexAttribArray(2);

		// Swap buffers
		glfwSwapBuffers(window);
		glfwPollEvents();

	} // Check if the ESC key was pressed or the window was closed
	while (glfwGetKey(window, GLFW_KEY_ESCAPE) != GLFW_PRESS &&
		glfwWindowShouldClose(window) == 0);

	delete[] g_centerpos_buffer_data;
	delete[] g_color_buffer_data;

	// Cleanup VBO and shader
	glDeleteBuffers(1, &vertexbuffer);
	glDeleteBuffers(1, &colorbuffer);
	glDeleteBuffers(1, &centerposbuffer);
	glDeleteVertexArrays(1, &VertexArrayID);
	glDeleteProgram(programID);

	// Close OpenGL window and terminate GLFW
	glfwTerminate();

	return 0;
}

