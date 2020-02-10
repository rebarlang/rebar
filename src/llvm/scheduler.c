// #include <stdlib.h>
#include "stdbool.h"

void* malloc(unsigned long);
void free(void*);

typedef void (*scheduled_task_func) (void*);

typedef struct
{
	scheduled_task_func func;
	void *state;
}
scheduled_task;

typedef struct node
{
	scheduled_task task;
	struct node *next;
}
scheduled_task_node;

scheduled_task_node *head = 0, *tail = 0;

scheduled_task_node *create_task_node(scheduled_task task)
{
	scheduled_task_node *new_node = (scheduled_task_node*)malloc(sizeof(scheduled_task_node));
	new_node->task = task;
	return new_node;
}

void schedule(scheduled_task task)
{
	scheduled_task_node *new_node = create_task_node(task);
	if (!head)
	{
		head = new_node;
		tail = new_node;
	}
	else // head != null
	{
		scheduled_task_node *old_tail = tail;
		old_tail->next = new_node;
		tail = new_node;
	}
}

void execute_all_tasks()
{
	while (true)
	{
		scheduled_task_node *node = head;
		if (!node)
		{
			break;
		}
		
		head = node->next;
		if (!head)
		{
			tail = 0;
		}		
		scheduled_task task = node->task;
		free(node);
		
		scheduled_task_func func = task.func;
		void *state = task.state;
		func(state);
	}
}