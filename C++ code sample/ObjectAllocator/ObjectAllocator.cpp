#include "ObjectAllocator.h"
#include <stdexcept> // for std::bad_alloc
#include <cstring>   // for std::memset
#include <vector>

// Constructor: Initialize the object allocator
ObjectAllocator::ObjectAllocator(size_t ObjectSize, const OAConfig& config)
    : objectSize_(ObjectSize), config_(config), FreeList_(nullptr)
{
    // Initialize the memory allocator
    // Allocate the initial memory page
    try
    {
        allocate_new_page();
    }
    catch (const std::bad_alloc&)
    {
        throw OAException(OAException::E_NO_MEMORY, "Out of memory");
    }
}

//Destructor: Free all allocated memory
ObjectAllocator::~ObjectAllocator()
     {
    // Free all allocated memory
    GenericObject* current = PageList_;
    while (current)
    {
        GenericObject* next = current->Next;
        delete[] reinterpret_cast<char*>(current);
        current = next;
    }
    PageList_ = nullptr;
    FreeList_ = nullptr;
}

void* ObjectAllocator::Allocate(const char* label)
{
    if (config_.UseCPPMemManager_)
    {
        // Use C++ memory manager to allocate memory
        void* object = new char[objectSize_];
        stats_.Allocations_++;
        stats_.ObjectsInUse_++;
        if (stats_.ObjectsInUse_ > stats_.MostObjects_)
            stats_.MostObjects_ = stats_.ObjectsInUse_;
        return object;
    }

    if (!FreeList_)
    {
        // If no free objects, allocate a new page
        allocate_new_page();
    }

    // Get an object from the free list
    GenericObject* freeObject = FreeList_;
    FreeList_ = FreeList_->Next;

    if (config_.DebugOn_)
    {
        // If debug mode is enabled, set allocated pattern
        std::memset(freeObject, ALLOCATED_PATTERN, objectSize_);
    }
    freeObject->Next = nullptr;
    stats_.Allocations_++;
    stats_.ObjectsInUse_++;
    if (stats_.ObjectsInUse_ > stats_.MostObjects_)
        stats_.MostObjects_ = stats_.ObjectsInUse_;
    //char* objectData = reinterpret_cast<char*>(freeObject) + config_.PadBytes_;
    char* objectData = reinterpret_cast<char*>(freeObject)- config_.HBlockInfo_.size_-config_.PadBytes_;
    if (config_.HBlockInfo_.type_ == OAConfig::hbBasic)
    {
        // Assign basic header values
        unsigned* allocNum = reinterpret_cast<unsigned*>(objectData);
        *allocNum = stats_.Allocations_;
        objectData += config_.HBlockInfo_.size_; // Move past the header
    }
    else if (config_.HBlockInfo_.type_ == OAConfig::hbExtended)
    {
        // Assign extended header values
        unsigned int* allocNum = reinterpret_cast<unsigned int*>(objectData);
        *allocNum = stats_.Allocations_;
        objectData += sizeof(unsigned int);

        unsigned short* useCount = reinterpret_cast<unsigned short*>(objectData);
        *useCount = 1;
        objectData += sizeof(unsigned short);

        char* flag = reinterpret_cast<char*>(objectData);
        *flag = 0;
        objectData += sizeof(char) + config_.HBlockInfo_.additional_;
    }
    else if (config_.HBlockInfo_.type_ == OAConfig::hbExternal)
    {
        // Assign external header values
        void** externalPtr = reinterpret_cast<void**>(objectData);
        *externalPtr = new MemBlockInfo();
        MemBlockInfo* memBlockInfo = static_cast<MemBlockInfo*>(*externalPtr);
        memBlockInfo->in_use = true;
        memBlockInfo->alloc_num = stats_.Allocations_;
        if (label)
        {
            memBlockInfo->label = const_cast<char*>(label);
        }
        else
        {
            memBlockInfo->label = nullptr;
        }
        objectData += config_.HBlockInfo_.size_;
    }
    objectData += config_.PadBytes_; // Move past the padding
    return objectData;
}


// Free an object
void ObjectAllocator::Free(void* object)
{
    if (config_.UseCPPMemManager_)
    {
        // Use C++ memory manager to free memory
        delete[] reinterpret_cast<char*>(object);
        stats_.Deallocations_++;
        stats_.ObjectsInUse_--;
        return;
    }

    // Calculate the actual address of object, considering padding and header size
    char* actualObject = reinterpret_cast<char*>(object) - config_.PadBytes_ - config_.HBlockInfo_.size_;
    

    if (config_.HBlockInfo_.type_ == OAConfig::hbBasic)
    {
        // Handle basic header
        unsigned* allocNum = reinterpret_cast<unsigned*>(actualObject);
        *allocNum = 0; // Reset allocation number
    }
    else if (config_.HBlockInfo_.type_ == OAConfig::hbExtended)
    {
        // Handle extended header
        unsigned int* allocNum = reinterpret_cast<unsigned int*>(actualObject);
        *allocNum = 0; // Reset allocation number

        unsigned short* useCount = reinterpret_cast<unsigned short*>(actualObject + sizeof(unsigned int));
        *useCount = 0; // Reset use count

        char* flag = reinterpret_cast<char*>(actualObject + sizeof(unsigned int) + sizeof(unsigned short));
        *flag = 0; // Reset flag
    }
    else if (config_.HBlockInfo_.type_ == OAConfig::hbExternal)
    {
        // Handle external header
        void** externalPtr = reinterpret_cast<void**>(actualObject);
        delete* externalPtr; // Delete the MemBlockInfo object
        *externalPtr = nullptr; // Reset external pointer
    }
    GenericObject* obj = reinterpret_cast<GenericObject*>(actualObject + config_.PadBytes_ + config_.HBlockInfo_.size_);
    if (config_.DebugOn_)
    {
        // Check for double free
        if (obj->Next != nullptr)
        {
            std::cout << "Double free detected" << std::endl;
            throw OAException(OAException::E_MULTIPLE_FREE, "Double free detected");
        }
        // Set freed pattern
        std::memset(actualObject + config_.PadBytes_ + config_.HBlockInfo_.size_, FREED_PATTERN, objectSize_);
    }

    // Put the object back on the free list with the correct offset
    //obj = reinterpret_cast<GenericObject*>(actualObject + config_.HBlockInfo_.size_ + config_.PadBytes_);
    obj->Next = FreeList_;
    FreeList_ = obj;

    stats_.Deallocations_++;
    stats_.ObjectsInUse_--;
}

// Free all empty pages
unsigned ObjectAllocator::FreeEmptyPages(void)
{
    unsigned freedPages = 0;
    GenericObject* prev = nullptr;
    GenericObject* current = PageList_;

    while (current)
    {
        bool allFree = true;
        char* page = reinterpret_cast<char*>(current);
        for (size_t i = 0; i < config_.ObjectsPerPage_; ++i)
        {
            char* object = page + sizeof(void*) + config_.HBlockInfo_.size_ + config_.PadBytes_ + i * (objectSize_ + 2 * config_.PadBytes_ + config_.HBlockInfo_.size_);
            if (reinterpret_cast<GenericObject*>(object)->Next == nullptr)
            {
                allFree = false;
                break;
            }
        }

        if (allFree)
        {
            // If all objects are free, release the page
            if (prev)
            {
                prev->Next = current->Next;
            }
            else
            {
                PageList_ = current->Next;
            }

            GenericObject* next = current->Next;
            delete[] reinterpret_cast<char*>(current);
            current = next;
            freedPages++;
            stats_.PagesInUse_--;
        }
        else
        {
            prev = current;
            current = current->Next;
        }
    }

    return freedPages;
}

// Dump memory in use
unsigned ObjectAllocator::DumpMemoryInUse(DUMPCALLBACK fn) const
{
    unsigned inUseCount = 0;
    GenericObject* currentPage = PageList_;

    while (currentPage)
    {
        char* pageStart = reinterpret_cast<char*>(currentPage);
        for (size_t i = 0; i < config_.ObjectsPerPage_; ++i)
        {
            char* object = pageStart + sizeof(void*) + config_.HBlockInfo_.size_ + config_.PadBytes_ +i * (objectSize_ + 2 * config_.PadBytes_ + config_.HBlockInfo_.size_);

            if (reinterpret_cast<GenericObject*>(object)->Next == nullptr)
            {
                fn(object, objectSize_);
                inUseCount++;
            }
        }
        currentPage = currentPage->Next;
    }

    return inUseCount;
}

// Validate the integrity of pages
unsigned ObjectAllocator::ValidatePages(VALIDATECALLBACK fn) const
{
    unsigned corruptedCount = 0;
    GenericObject* currentPage = PageList_;

    while (currentPage)
    {
        char* pageStart = reinterpret_cast<char*>(currentPage);
        for (size_t i = 0; i < config_.ObjectsPerPage_; ++i)
        {
            char* object = pageStart + sizeof(void*) + config_.HBlockInfo_.size_ + config_.PadBytes_ + i * (objectSize_ + 2 * config_.PadBytes_ + config_.HBlockInfo_.size_);
            char* leftPad = object - config_.PadBytes_;
            char* rightPad = object + objectSize_;
            bool corrupted = false;
            for (unsigned j = 0; j < config_.PadBytes_; ++j)
            {
                if (leftPad[j] != (char)PAD_PATTERN || rightPad[j] != (char)PAD_PATTERN)
                {
                    corrupted = true;
                    break;
                }
            }
            if (corrupted)
            {
                fn(object, objectSize_);
                corruptedCount++;
            }
        }
        currentPage = currentPage->Next;
    }

    return corruptedCount;
}

// Check if extra credit features are implemented
bool ObjectAllocator::ImplementedExtraCredit(void)
{
    // Check if extra credit features are implemented
    return false;
}

// Set debug state
void ObjectAllocator::SetDebugState(bool State)
{
    // Set debug state
    config_.DebugOn_ = State;
}

// Get the free list
const void* ObjectAllocator::GetFreeList(void) const
{
    return FreeList_;
}

// Get the page list
const void* ObjectAllocator::GetPageList(void) const
{
    return PageList_;
}

// Get the configuration
OAConfig ObjectAllocator::GetConfig(void) const
{
    return config_;
}

// Get the statistics
OAStats ObjectAllocator::GetStats(void) const
{
    return stats_;
}


// Allocate a new page
void ObjectAllocator::allocate_new_page()
{
    if (config_.MaxPages_ != 0 && stats_.PagesInUse_ >= config_.MaxPages_)
    {
        throw OAException(OAException::E_NO_PAGES, "Max pages reached");
    }
    size_t Offset = objectSize_ + 2 * config_.PadBytes_ + config_.HBlockInfo_.size_;

    // Adjust alignment
    if (config_.Alignment_ > 0)
    {
        //Offset = (Offset + config_.Alignment_ - 1) & ~(config_.Alignment_ - 1);
    }

    size_t pageSize = config_.ObjectsPerPage_ * Offset + sizeof(void*);
    char* newPage = new char[pageSize];
    //std::memset(newPage, UNALLOCATED_PATTERN, pageSize);

    // Add the new page to the PageList_ linked list
    GenericObject* page = reinterpret_cast<GenericObject*>(newPage);
    page->Next = PageList_;
    PageList_ = page;

    char* object = newPage + sizeof(void*);
    for (size_t i = 0; i < config_.ObjectsPerPage_; ++i)
    {
        // Initialize header block
        if (config_.DebugOn_)
        {
            std::memset(object + config_.HBlockInfo_.size_ + config_.PadBytes_, UNALLOCATED_PATTERN, objectSize_);
            if (config_.PadBytes_ > 0)
            {
                std::memset(object + config_.HBlockInfo_.size_, PAD_PATTERN, config_.PadBytes_);
                std::memset(object + config_.HBlockInfo_.size_ + config_.PadBytes_ + objectSize_, PAD_PATTERN, config_.PadBytes_);
            }
        }
        
        if (config_.HBlockInfo_.type_ == OAConfig::hbBasic)
        {
            unsigned* allocNum = reinterpret_cast<unsigned*>(object + 0);
            *allocNum = 0;
        }
        else if (config_.HBlockInfo_.type_ == OAConfig::hbExtended)
        {
            unsigned int* allocNum = reinterpret_cast<unsigned int*>(object + 0);
            *allocNum = 0;

            unsigned short* useCount = reinterpret_cast<unsigned short*>(object + 0 + sizeof(unsigned int));
            *useCount = 0;

            char* flag = reinterpret_cast<char*>(object + 0 + sizeof(unsigned int) + sizeof(unsigned short));
            *flag = 0;
        }
        else if (config_.HBlockInfo_.type_ == OAConfig::hbExternal)
        {
            void** externalPtr = reinterpret_cast<void**>(object);
            *externalPtr = nullptr;
        }
        

        put_on_freelist(object+ config_.HBlockInfo_.size_+config_.PadBytes_);

        
        object += Offset;
    }

    stats_.PagesInUse_++;
    stats_.ObjectSize_ = objectSize_;
    stats_.PageSize_ = pageSize;
}


// Put the object on the free list
void ObjectAllocator::put_on_freelist(void* object)
{
    GenericObject* obj = reinterpret_cast<GenericObject*>(object);
    obj->Next = FreeList_;
    FreeList_ = obj;
}
