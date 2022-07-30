#ifndef BUFFER_HPP_INCLUDED
#define BUFFER_HPP_INCLUDED

//
//  buffer.hpp
//
//  Copyright (c) 2011 Boris Kolpackov
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
//  Simple memory buffer abstraction. Version 1.0.0.
//

#include <cstddef>   // std::size_t
#include <cstring>   // std::memcpy, std::memcmp, std::memset, std::memchr
#include <stdexcept> // std::out_of_range, std::invalid_argument

class buffer
{
public:
	typedef std::size_t size_type;

	static const size_type npos = static_cast<size_type>(-1);

	~buffer();

	explicit buffer(size_type size = 0);
	buffer(size_type size, size_type capacity);
	buffer(const void* data, size_type size);
	buffer(const void* data, size_type size, size_type capacity);
	buffer(void* data, size_type size, size_type capacity, bool assume_ownership);

	buffer(const buffer&);
	buffer& operator=(const buffer&);

	void swap(buffer&);
	char* detach();

	void assign(const void* data, size_type size);
	void assign(void* data, size_type size, size_type capacity, bool assume_ownership);
	void append(const buffer&);
	void append(const void* data, size_type size);
	void fill(char value = 0);

	size_type size() const;
	bool size(size_type);
	size_type capacity() const;
	bool capacity(size_type);
	bool empty() const;
	void clear();

	char* data();
	const char* data() const;

	char& operator[](size_type);
	char operator[](size_type) const;
	char& at(size_type);
	char at(size_type) const;

	size_type find(char, size_type pos = 0) const;
	size_type rfind(char, size_type pos = npos) const;

private:
	char* data_;
	size_type size_;
	size_type capacity_;
	bool free_;
};

bool operator==(const buffer&, const buffer&);
bool operator!=(const buffer&, const buffer&);

//
// Implementation.
//
inline buffer::~buffer()
{
	if (free_)
		delete[] data_;
}

inline buffer::buffer(size_type s) : free_(true)
{
	data_ = (s != 0 ? new char[s] : 0);
	size_ = capacity_ = s;
}

inline buffer::buffer(size_type s, size_type c) : free_(true)
{
	if (s > c)
		throw std::invalid_argument("size greater than capacity");

	data_ = (c != 0 ? new char[c] : 0);
	size_ = s;
	capacity_ = c;
}

inline buffer::buffer(const void* d, size_type s) : free_(true)
{
	if (s != 0)
	{
		data_ = new char[s];
		std::memcpy(data_, d, s);
	}
	else
		data_ = 0;

	size_ = capacity_ = s;
}

inline buffer::buffer(const void* d, size_type s, size_type c) : free_(true)
{
	if (s > c)
		throw std::invalid_argument("size greater than capacity");

	if (c != 0)
	{
		data_ = new char[c];

		if (s != 0)
			std::memcpy(data_, d, s);
	}
	else
		data_ = 0;

	size_ = s;
	capacity_ = c;
}

inline buffer::buffer(void* d, size_type s, size_type c, bool own) : data_(static_cast<char*>(d)), size_(s), capacity_(c), free_(own)
{
	if (s > c)
		throw std::invalid_argument("size greater than capacity");
}

inline buffer::buffer(const buffer& x) : free_(true)
{
	if (x.capacity_ != 0)
	{
		data_ = new char[x.capacity_];

		if (x.size_ != 0)
			std::memcpy(data_, x.data_, x.size_);
	}
	else
		data_ = 0;

	size_ = x.size_;
	capacity_ = x.capacity_;
}

inline buffer& buffer::operator=(const buffer& x)
{
	if (&x != this)
	{
		if (x.size_ > capacity_)
		{
			if (free_)
				delete[] data_;

			data_ = new char[x.capacity_];
			capacity_ = x.capacity_;
			free_ = true;
		}

		if (x.size_ != 0)
			std::memcpy(data_, x.data_, x.size_);

		size_ = x.size_;
	}

	return *this;
}

inline void buffer::swap(buffer& x)
{
	char* d(x.data_);
	size_type s(x.size_);
	size_type c(x.capacity_);
	bool f(x.free_);

	x.data_ = data_;
	x.size_ = size_;
	x.capacity_ = capacity_;
	x.free_ = free_;

	data_ = d;
	size_ = s;
	capacity_ = c;
	free_ = f;
}

inline char* buffer::detach()
{
	char* r(data_);

	data_ = 0;
	size_ = 0;
	capacity_ = 0;

	return r;
}

inline void buffer::assign(const void* d, size_type s)
{
	if (s > capacity_)
	{
		if (free_)
			delete[] data_;

		data_ = new char[s];
		capacity_ = s;
		free_ = true;
	}

	if (s != 0)
		std::memcpy(data_, d, s);

	size_ = s;
}

inline void buffer::assign(void* d, size_type s, size_type c, bool own)
{
	if (free_)
		delete[] data_;

	data_ = static_cast<char*>(d);
	size_ = s;
	capacity_ = c;
	free_ = own;
}

inline void buffer::append(const buffer& b)
{
	append(b.data(), b.size());
}

inline void buffer::append(const void* d, size_type s)
{
	if (s != 0)
	{
		size_type ns(size_ + s);

		if (capacity_ < ns)
			capacity(ns);

		std::memcpy(data_ + size_, d, s);
		size_ = ns;
	}
}

inline void buffer::fill(char v)
{
	if (size_ > 0)
		std::memset(data_, v, size_);
}

inline buffer::size_type buffer::size() const
{
	return size_;
}

inline bool buffer::size(size_type s)
{
	bool r(false);

	if (capacity_ < s)
		r = capacity(s);

	size_ = s;
	return r;
}

inline buffer::size_type buffer::capacity() const
{
	return capacity_;
}

inline bool buffer::capacity(size_type c)
{
	// Ignore capacity decrease requests.
	//
	if (capacity_ >= c)
		return false;

	char* d(new char[c]);

	if (size_ != 0)
		std::memcpy(d, data_, size_);

	if (free_)
		delete[] data_;

	data_ = d;
	capacity_ = c;
	free_ = true;

	return true;
}

inline bool buffer::empty() const
{
	return size_ == 0;
}

inline void buffer::clear()
{
	size_ = 0;
}

inline char* buffer::data()
{
	return data_;
}

inline const char* buffer::data() const
{
	return data_;
}

inline char& buffer::operator[](size_type i)
{
	return data_[i];
}

inline char buffer::operator[](size_type i) const
{
	return data_[i];
}

inline char& buffer::at(size_type i)
{
	if (i >= size_)
		throw std::out_of_range("index out of range");

	return data_[i];
}

inline char buffer::at(size_type i) const
{
	if (i >= size_)
		throw std::out_of_range("index out of range");

	return data_[i];
}

inline buffer::size_type buffer::find(char v, size_type pos) const
{
	if (size_ == 0 || pos >= size_)
		return npos;

	char* p(static_cast<char*>(std::memchr(data_ + pos, v, size_ - pos)));
	return p != 0 ? static_cast<size_type>(p - data_) : npos;
}

inline buffer::size_type buffer::rfind(char v, size_type pos) const
{
	// memrchr() is not standard.
	//
	if (size_ != 0)
	{
		size_type n(size_);

		if (--n > pos)
			n = pos;

		for (++n; n-- != 0;)
			if (data_[n] == v)
				return n;
	}

	return npos;
}

inline bool operator==(const buffer& a, const buffer& b)
{
	return a.size() == b.size() && std::memcmp(a.data(), b.data(), a.size()) == 0;
}

inline bool operator!=(const buffer& a, const buffer& b)
{
	return !(a == b);
}

#endif // BUFFER_HPP_INCLUDED