#include "stdbool.h"

typedef signed char int8_t;
typedef char uint8_t;
typedef short int16_t;
typedef unsigned short uint16_t;
typedef long int32_t;
typedef unsigned long uint32_t;
typedef long long int64_t;
typedef unsigned long long uint64_t;

extern void output_string(const char *beginPtr, int length);
void output_uint64_core(uint64_t, bool);

const char trueString[5] = "true";
const char falseString[6] = "false";

void output_bool(bool value)
{
	const char *str = value ? trueString : falseString;
	int length = value ? 4 : 5;
	output_string(str, length);
}

void output_int64(int64_t value)
{
	bool isPositive = value > 0;
	uint64_t abs = isPositive ? (uint64_t)value : (uint64_t)-value;
	output_uint64_core(abs, !isPositive);
}

void output_uint64(uint64_t value)
{
	output_uint64_core(value, false);
}

void output_int8(int8_t value)
{
	output_int64((int64_t)value);
}

void output_uint8(int8_t value)
{
	output_uint64((uint64_t)value);
}

void output_int16(int16_t value)
{
	output_int64((int64_t)value);
}

void output_uint16(int16_t value)
{
	output_uint64((uint64_t)value);
}

void output_int32(int32_t value)
{
	output_int64((int64_t)value);
}

void output_uint32(int32_t value)
{
	output_uint64((uint64_t)value);
}

void output_uint64_core(uint64_t value, bool negative)
{
	char buffer[20];
	bool isPositive = value > 0;
	unsigned long long currentValue = value;
	int length = 0, position = 19;
	while (currentValue > 0)
	{
		unsigned long long mod = currentValue % 10;
		char numChar = '0' + (char)mod;
		buffer[position] = numChar;
		currentValue /= 10;
		++length;
		--position;
	}
	if (length == 0)
	{
		buffer[19] = '0';
		length = 1;
	}
	else if (negative)
	{
		buffer[position] = '-';
		++length;
	}
	output_string(&buffer[20 - length], length);
}